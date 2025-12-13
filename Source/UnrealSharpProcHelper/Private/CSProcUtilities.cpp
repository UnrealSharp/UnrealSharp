#include "CSProcUtilities.h"
#include "UnrealSharpProcHelper.h"
#include "Misc/App.h"
#include "Misc/Paths.h"
#include "Interfaces/IPluginManager.h"
#include "Misc/MessageDialog.h"

bool UCSProcUtilities::InvokeCommand(const FString& ProgramPath, const FString& Arguments, int32& OutReturnCode, FString& Output, const FString* InWorkingDirectory)
{
	double StartTime = FPlatformTime::Seconds();
	FString ProgramName = FPaths::GetBaseFilename(ProgramPath);
	FString WorkingDirectory = InWorkingDirectory ? *InWorkingDirectory : FPaths::GetPath(ProgramPath);
	
	void* PipeRead = nullptr;
	void* PipeWrite = nullptr;

	FPlatformProcess::CreatePipe(PipeRead, PipeWrite);

	OutReturnCode = -1;
	FProcHandle ProcHandle = FPlatformProcess::CreateProc(*ProgramPath, *Arguments, true, true, true, nullptr, 0, *WorkingDirectory, PipeWrite, PipeRead);
	
	if (ProcHandle.IsValid())
	{
		while (FPlatformProcess::IsProcRunning(ProcHandle))
		{
			FString ThisRead = FPlatformProcess::ReadPipe(PipeRead);
			Output += ThisRead;
		}

		Output += FPlatformProcess::ReadPipe(PipeRead);
		FPlatformProcess::GetProcReturnCode(ProcHandle, &OutReturnCode);
	}
	else
	{
		UE_LOGFMT(LogUnrealSharpProcHelper, Error, "Failed to start process: {0} with arguments: {1}", *ProgramName, *Arguments);
	}

	FPlatformProcess::ClosePipe(PipeRead, PipeWrite);

	if (OutReturnCode != 0)
	{
		FText DialogText = FText::FromString(FString::Printf(TEXT("%s task failed: \n %s"), *ProgramName, *Output));
		FMessageDialog::Open(EAppMsgType::Ok, DialogText);
		return false;
	}

	double EndTime = FPlatformTime::Seconds();
	double ElapsedTime = EndTime - StartTime;
	UE_LOGFMT(LogUnrealSharpProcHelper, Display, "{0} task completed in {1} seconds.", *ProgramName, ElapsedTime);
	return true;
}

bool UCSProcUtilities::InvokeUnrealSharpBuildTool(const FString& BuildAction, const TMap<FString, FString>& AdditionalArguments)
{
	FString PluginFolder = FPaths::ConvertRelativePathToFull(IPluginManager::Get().FindPlugin(UE_PLUGIN_NAME)->GetBaseDir());
	FString DotNetPath = GetDotNetExecutablePath();

	FString Args;
	Args += FString::Printf(TEXT("\"%s\""), *FPaths::ConvertRelativePathToFull(GetUnrealSharpBuildToolPath()));
	Args += FString::Printf(TEXT(" --Action %s"), *BuildAction);
	Args += FString::Printf(TEXT(" --EngineDirectory \"%s\""), *FPaths::ConvertRelativePathToFull(FPaths::EngineDir()));
	Args += FString::Printf(TEXT(" --ProjectDirectory \"%s\""), *FPaths::ConvertRelativePathToFull(FPaths::ProjectDir()));
	Args += FString::Printf(TEXT(" --ProjectName %s"), FApp::GetProjectName());
	Args += FString::Printf(TEXT(" --PluginDirectory \"%s\""), *PluginFolder);
	Args += FString::Printf(TEXT(" --DotNetPath \"%s\""), *DotNetPath);

	if (AdditionalArguments.Num())
	{
		Args += TEXT(" --AdditionalArgs");
		for (const TPair<FString, FString>& Argument : AdditionalArguments)
		{
			Args += FString::Printf(TEXT(" %s=%s"), *Argument.Key, *Argument.Value);
		}
	}

	int32 ReturnCode = 0;
	FString Output;
	FString WorkingDirectory = GetPluginAssembliesPath();
	return InvokeCommand(DotNetPath, Args, ReturnCode, Output, &WorkingDirectory);
}

FString UCSProcUtilities::GetLatestHostFxrPath()
{
	FString DotNetRoot = GetDotNetDirectory();
	FString HostFxrRoot = FPaths::Combine(DotNetRoot, "host", "fxr");

	TArray<FString> Folders;
	IFileManager::Get().FindFiles(Folders, *(HostFxrRoot / "*"), true, true);

	FString HighestVersion = "0.0.0";
	for (const FString &Folder : Folders)
	{
		if (Folder > HighestVersion)
		{
			HighestVersion = Folder;
		}
	}

	if (HighestVersion == "0.0.0")
	{
		UE_LOG(LogUnrealSharpProcHelper, Fatal, TEXT("Failed to find hostfxr version in %s"), *HostFxrRoot);
		return "";
	}

	if (HighestVersion < DOTNET_MAJOR_VERSION)
	{
		UE_LOG(LogUnrealSharpProcHelper, Fatal, TEXT("Hostfxr version %s is less than the required version %s"), *HighestVersion, TEXT(DOTNET_MAJOR_VERSION));
		return "";
	}

#ifdef _WIN32
	return FPaths::Combine(HostFxrRoot, HighestVersion, HOSTFXR_WINDOWS);
#elif defined(__APPLE__)
	return FPaths::Combine(HostFxrRoot, HighestVersion, HOSTFXR_MAC);
#else
	return FPaths::Combine(HostFxrRoot, HighestVersion, HOSTFXR_LINUX);
#endif
}

FString UCSProcUtilities::GetRuntimeHostPath()
{
#if WITH_EDITOR
	return GetLatestHostFxrPath();
#else
#ifdef _WIN32
	return FPaths::Combine(GetPluginAssembliesPath(), HOSTFXR_WINDOWS);
#elif defined(__APPLE__)
	return FPaths::Combine(GetPluginAssembliesPath(), HOSTFXR_MAC);
#else
	return FPaths::Combine(GetPluginAssembliesPath(), HOSTFXR_LINUX);
#endif
#endif
}

FString UCSProcUtilities::GetPathToManagedSolution()
{
	static FString SolutionPath = GetScriptFolderDirectory() / GetUserManagedProjectName() + ".sln";
	return SolutionPath;
}

FString& UCSProcUtilities::GetManagedBinaries()
{
	static FString ManagedBinaries = FPaths::Combine("Binaries", "Managed", DOTNET_DISPLAY_NAME);
	return ManagedBinaries;
}

FString UCSProcUtilities::GetPluginAssembliesPath()
{
#if WITH_EDITOR
	return FPaths::Combine(GetPluginDirectory(), GetManagedBinaries());
#else
	return GetUserAssemblyDirectory();
#endif
}

FString UCSProcUtilities::GetUnrealSharpPluginsPath()
{
	return GetPluginAssembliesPath() / "UnrealSharp.Plugins.dll";
}

bool UCSProcUtilities::BuildUserSolution()
{
	TMap<FString, FString> Arguments;
	Arguments.Add("OutputPath", GetUserAssemblyDirectory());
	return InvokeUnrealSharpBuildTool(BUILD_ACTION_BUILD_EMIT_LOAD_ORDER, Arguments);
}

FString UCSProcUtilities::GetRuntimeConfigPath()
{
	return GetPluginAssembliesPath() / "UnrealSharp.runtimeconfig.json";
}

FString UCSProcUtilities::GetUserAssemblyDirectory()
{
	return FPaths::ConvertRelativePathToFull(FPaths::Combine(FPaths::ProjectDir(), GetManagedBinaries()));
}

FString UCSProcUtilities::GetUnrealSharpMetadataPath()
{
	return FPaths::Combine(GetUserAssemblyDirectory(), "AssemblyLoadOrder.json");
}

void UCSProcUtilities::GetProjectNamesByLoadOrder(TArray<FString>& UserProjectNames, const bool bIncludeGlue)
{
	const FString ProjectMetadataPath = GetUnrealSharpMetadataPath();

	if (!FPaths::FileExists(ProjectMetadataPath))
	{
		// Can be null at the start of the project.
		return;
	}

	FString JsonString;
	if (!FFileHelper::LoadFileToString(JsonString, *ProjectMetadataPath))
	{
		UE_LOG(LogUnrealSharpProcHelper, Fatal, TEXT("Failed to load UnrealSharp metadata file at: %s"), *ProjectMetadataPath);
		return;
	}

	TSharedPtr<FJsonObject> JsonObject;
	if (!FJsonSerializer::Deserialize(TJsonReaderFactory<>::Create(JsonString), JsonObject) || !JsonObject.IsValid())
	{
		UE_LOG(LogUnrealSharpProcHelper, Fatal, TEXT("Failed to parse UnrealSharp metadata at: %s"), *ProjectMetadataPath);
		return;
	}

	const TArray<TSharedPtr<FJsonValue>>* LoadOrderArray;
	if (!JsonObject->TryGetArrayField(TEXT("LoadOrder"), LoadOrderArray))
	{
		UE_LOG(LogUnrealSharpProcHelper, Fatal, TEXT("Failed to find LoadOrder array in UnrealSharp metadata at: %s"), *ProjectMetadataPath);
		return;
	}

	for (const TSharedPtr<FJsonValue>& OrderEntry : *LoadOrderArray)
	{
		FString ProjectName = OrderEntry->AsString();

		if (!bIncludeGlue && ProjectName.EndsWith(TEXT(".Glue")))
		{
			continue;
		}

		UserProjectNames.Add(OrderEntry->AsString());
	}
}


void UCSProcUtilities::GetAssemblyPathsByLoadOrder(TArray<FString>& AssemblyPaths, const bool bIncludeGlue)
{
	FString AbsoluteFolderPath = GetUserAssemblyDirectory();

	TArray<FString> ProjectNames;
	GetProjectNamesByLoadOrder(ProjectNames, bIncludeGlue);

	for (const FString& ProjectName : ProjectNames)
	{
		const FString AssemblyPath = FPaths::Combine(AbsoluteFolderPath, ProjectName + TEXT(".dll"));
		AssemblyPaths.Add(AssemblyPath);
	}
}

void UCSProcUtilities::GetAllProjectPaths(TArray<FString>& ProjectPaths, bool bIncludeProjectGlue)
{
	// Use the FileManager to find files matching the pattern
	IFileManager::Get().FindFilesRecursive(ProjectPaths,
		*GetScriptFolderDirectory(),
		TEXT("*.csproj"),
		true,
		false,
		false);

    TArray<FString> PluginFilePaths;
    IPluginManager::Get().FindPluginsUnderDirectory(FPaths::ProjectPluginsDir(), PluginFilePaths);
	
    for (const FString& PluginFilePath : PluginFilePaths)
    {
        FString ScriptDirectory = FPaths::GetPath(PluginFilePath) / "Script";
        IFileManager::Get().FindFilesRecursive(ProjectPaths,
            *ScriptDirectory,
            TEXT("*.csproj"),
            true,
            false,
            false);
    }

	for (int32 i = ProjectPaths.Num() - 1; i >= 0; i--)
	{
		if (bIncludeProjectGlue || !ProjectPaths[i].EndsWith("Glue.csproj"))
		{
			continue;
		}

		ProjectPaths.RemoveAt(i);
	}
}

FString UCSProcUtilities::GetUnrealSharpBuildToolPath()
{
#if PLATFORM_WINDOWS
	return FPaths::ConvertRelativePathToFull(GetPluginAssembliesPath() / "UnrealSharpBuildTool.dll");
#else
	return FPaths::ConvertRelativePathToFull(GetPluginAssembliesPath() / "UnrealSharpBuildTool");
#endif
}

FString UCSProcUtilities::GetDotNetDirectory()
{
	const FString PathVariable = FPlatformMisc::GetEnvironmentVariable(TEXT("PATH"));

	TArray<FString> Paths;
	PathVariable.ParseIntoArray(Paths, FPlatformMisc::GetPathVarDelimiter());

#if defined(_WIN32)
	FString PathDotnet = "Program Files\\dotnet\\";
#elif defined(__APPLE__)
	FString PathDotnet = "/usr/local/share/dotnet/";
	return PathDotnet;
#endif
	for (FString &Path : Paths)
	{
		if (!Path.Contains(PathDotnet))
		{
			continue;
		}

		if (!FPaths::DirectoryExists(Path))
		{
			UE_LOG(LogUnrealSharpProcHelper, Warning, TEXT("Found path to DotNet, but the directory doesn't exist: %s"), *Path);
			break;
		}

		return Path;
	}
	return "";
}

FString UCSProcUtilities::GetDotNetExecutablePath()
{
#if defined(_WIN32)
	return GetDotNetDirectory() + "dotnet.exe";
#else
	return GetDotNetDirectory() + "dotnet";
#endif
}

FString& UCSProcUtilities::GetPluginDirectory()
{
	static FString PluginDirectory;

	if (PluginDirectory.IsEmpty())
	{
		TSharedPtr<IPlugin> Plugin = IPluginManager::Get().FindPlugin(UE_PLUGIN_NAME);
		check(Plugin);
		PluginDirectory = Plugin->GetBaseDir();
	}

	return PluginDirectory;
}

FString UCSProcUtilities::GetUnrealSharpDirectory()
{
	return FPaths::Combine(GetPluginDirectory(), "Managed", "UnrealSharp");
}

FString UCSProcUtilities::GetGeneratedClassesDirectory()
{
	return FPaths::Combine(GetUnrealSharpDirectory(), "UnrealSharp", "Generated");
}

const FString& UCSProcUtilities::GetScriptFolderDirectory()
{
	static FString ScriptFolderDirectory = FPaths::ProjectDir() / "Script";
	return ScriptFolderDirectory;
}

const FString& UCSProcUtilities::GetPluginsDirectory()
{
    static FString PluginsDirectory = FPaths::ProjectDir() / "Plugins";
    return PluginsDirectory;
}

const FString& UCSProcUtilities::GetProjectGlueFolderPath()
{
	static FString ProjectGlueFolderPath = GetScriptFolderDirectory() / FApp::GetProjectName() + TEXT(".Glue");
	return ProjectGlueFolderPath;
}

FString UCSProcUtilities::GetUserManagedProjectName()
{
	return FString::Printf(TEXT("Managed%s"), FApp::GetProjectName());
}
