#include "CSProcHelper.h"
#include "UnrealSharpProcHelper.h"
#include "XmlFile.h"
#include "XmlNode.h"
#include "Misc/App.h"
#include "Misc/Paths.h"
#include "Interfaces/IPluginManager.h"
#include "Misc/MessageDialog.h"

bool FCSProcHelper::InvokeCommand(const FString& ProgramPath, const FString& Arguments, int32& OutReturnCode, FString& Output, const FString* InWorkingDirectory)
{
	double StartTime = FPlatformTime::Seconds();
	FString ProgramName = FPaths::GetBaseFilename(ProgramPath);
	FString WorkingDirectory = InWorkingDirectory ? *InWorkingDirectory : FPaths::GetPath(ProgramPath);

	FString ErrorMessage;
	FPlatformProcess::ExecProcess(*ProgramPath, *Arguments, &OutReturnCode, &Output, &ErrorMessage, *WorkingDirectory);

	if (OutReturnCode != 0)
	{
		UE_LOG(LogUnrealSharpProcHelper, Error, TEXT("%s task failed (Args: %s) with return code %d. Error: %s"), *ProgramName, *Arguments, OutReturnCode, *Output)

		FText DialogText = FText::FromString(FString::Printf(TEXT("%s task failed: \n %s"), *ProgramName, *Output));
		FMessageDialog::Open(EAppMsgType::Ok, DialogText);
		return false;
	}

	double EndTime = FPlatformTime::Seconds();
	double ElapsedTime = EndTime - StartTime;
	UE_LOG(LogUnrealSharpProcHelper, Log, TEXT("%s with args (%s) took %f seconds to execute."), *ProgramName, *Arguments, ElapsedTime);
	return true;
}

bool FCSProcHelper::InvokeUnrealSharpBuildTool(const FString& BuildAction, const TMap<FString, FString>& AdditionalArguments)
{
	FString PluginFolder = FPaths::ConvertRelativePathToFull(IPluginManager::Get().FindPlugin(UE_PLUGIN_NAME)->GetBaseDir());
	FString DotNetPath = GetDotNetExecutablePath();

	FString Args;
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
	return InvokeCommand(GetUnrealSharpBuildToolPath(), Args, ReturnCode, Output, &WorkingDirectory);
}

FString FCSProcHelper::GetLatestHostFxrPath()
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

FString FCSProcHelper::GetRuntimeHostPath()
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

FString FCSProcHelper::GetPathToSolution()
{
	static FString SolutionPath = GetScriptFolderDirectory() / GetUserManagedProjectName() + ".sln";
	return SolutionPath;
}

FString FCSProcHelper::GetPluginAssembliesPath()
{
#if WITH_EDITOR
	return FPaths::Combine(GetPluginDirectory(), "Binaries", "Managed");
#else
	return GetUserAssemblyDirectory();
#endif
}

FString FCSProcHelper::GetUnrealSharpPluginsPath()
{
	return GetPluginAssembliesPath() / "UnrealSharp.Plugins.dll";
}

FString FCSProcHelper::GetRuntimeConfigPath()
{
	return GetPluginAssembliesPath() / "UnrealSharp.runtimeconfig.json";
}

FString FCSProcHelper::GetUserAssemblyDirectory()
{
	return FPaths::ConvertRelativePathToFull(FPaths::Combine(FPaths::ProjectDir(), "Binaries", "Managed"));
}

FString FCSProcHelper::GetUnrealSharpMetadataPath()
{
	return FPaths::Combine(GetUserAssemblyDirectory(), "UnrealSharp.assemblyloadorder.json");
}

void FCSProcHelper::GetProjectNamesByLoadOrder(TArray<FString>& UserProjectNames, const bool bIncludeGlue)
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

	for (const TSharedPtr<FJsonValue>& OrderEntry : JsonObject->GetArrayField(TEXT("AssemblyLoadingOrder")))
	{
		FString ProjectName = OrderEntry->AsString();

		if (!bIncludeGlue && ProjectName.EndsWith(TEXT("Glue")))
		{
			continue;
		}

		UserProjectNames.Add(OrderEntry->AsString());
	}
}


void FCSProcHelper::GetAssemblyPathsByLoadOrder(TArray<FString>& AssemblyPaths, const bool bIncludeGlue)
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

void FCSProcHelper::GetAllProjectPaths(TArray<FString>& ProjectPaths, bool bIncludeProjectGlue)
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

bool FCSProcHelper::IsProjectReloadable(FStringView ProjectPath)
{
    FXmlFile ProjectFile(ProjectPath.GetData());
    if (!ProjectFile.IsValid())
    {
        UE_LOG(LogUnrealSharpProcHelper, Warning, TEXT("Failed to parse project file as XML: %s"),
            ProjectPath.GetData());
        return true;
    }

    const FXmlNode* RootNode = ProjectFile.GetRootNode();
    if (!RootNode)
    {
        return true;
    }

    // Look through all PropertyGroup elements
    for (const TArray<FXmlNode*>& ProjectNodes = RootNode->GetChildrenNodes();
        const FXmlNode* Node : ProjectNodes)
    {
        if (Node->GetTag() == TEXT("PropertyGroup"))
        {
            if (const FXmlNode* RoslynComponentNode = Node->FindChildNode(TEXT("ExcludeFromWeaver"));
                RoslynComponentNode &&
                RoslynComponentNode->GetContent().Equals(TEXT("true"), ESearchCase::IgnoreCase))
            {
                return false;
            }
        }
    }

    return true;
}

FString FCSProcHelper::GetUnrealSharpBuildToolPath()
{
#if PLATFORM_WINDOWS
	return FPaths::ConvertRelativePathToFull(GetPluginAssembliesPath() / "UnrealSharpBuildTool.exe");
#else
	return FPaths::ConvertRelativePathToFull(GetPluginAssembliesPath() / "UnrealSharpBuildTool");
#endif
}

FString FCSProcHelper::GetDotNetDirectory()
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

FString FCSProcHelper::GetDotNetExecutablePath()
{
#if defined(_WIN32)
	return GetDotNetDirectory() + "dotnet.exe";
#else
	return GetDotNetDirectory() + "dotnet";
#endif
}

FString& FCSProcHelper::GetPluginDirectory()
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

FString FCSProcHelper::GetUnrealSharpDirectory()
{
	return FPaths::Combine(GetPluginDirectory(), "Managed", "UnrealSharp");
}

FString FCSProcHelper::GetGeneratedClassesDirectory()
{
	return FPaths::Combine(GetUnrealSharpDirectory(), "UnrealSharp", "Generated");
}

const FString& FCSProcHelper::GetScriptFolderDirectory()
{
	static FString ScriptFolderDirectory = FPaths::ProjectDir() / "Script";
	return ScriptFolderDirectory;
}

const FString& FCSProcHelper::GetPluginsDirectory()
{
    static FString PluginsDirectory = FPaths::ProjectDir() / "Plugins";
    return PluginsDirectory;
}

const FString& FCSProcHelper::GetProjectGlueFolderPath()
{
	static FString ProjectGlueFolderPath = GetScriptFolderDirectory() / FApp::GetProjectName() + TEXT(".Glue");
	return ProjectGlueFolderPath;
}

FString FCSProcHelper::GetUserManagedProjectName()
{
	return FString::Printf(TEXT("Managed%s"), FApp::GetProjectName());
}
