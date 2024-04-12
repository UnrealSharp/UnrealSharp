#include "CSProcHelper.h"
#include "UnrealSharpProcHelper.h"
#include "Misc/App.h"
#include "Misc/Paths.h"
#include "Interfaces/IPluginManager.h"
#include "Misc/MessageDialog.h"

bool FCSProcHelper::InvokeCommand(const FString& ProgramPath, const FString& Arguments, int32& OutReturnCode, FString& Output, FString* InWorkingDirectory)
{
	double StartTime = FPlatformTime::Seconds();
	FString ProgramName = FPaths::GetBaseFilename(ProgramPath);
	
	if (!FPaths::FileExists(ProgramPath))
	{
		FString DialogText = FString::Printf(TEXT("Failed to find %s at %s"), *ProgramName, *ProgramPath);
		UE_LOG(LogUnrealSharpProcHelper, Error, TEXT("%s"), *DialogText);
		
		FMessageDialog::Open(EAppMsgType::Ok, FText::FromString(DialogText));
		return false;
	}
		
	const bool bLaunchDetached = false;
	const bool bLaunchHidden = true;
	const bool bLaunchReallyHidden = bLaunchHidden;
	
	void* ReadPipe;
	void* WritePipe;
	FPlatformProcess::CreatePipe(ReadPipe, WritePipe);

	FString WorkingDirectory = InWorkingDirectory ? *InWorkingDirectory : FPaths::GetPath(ProgramPath);
	
	FProcHandle ProcHandle = FPlatformProcess::CreateProc(*ProgramPath,
	                                                      *Arguments,
	                                                      bLaunchDetached,
	                                                      bLaunchHidden,
	                                                      bLaunchReallyHidden,
	                                                      NULL, 0,
	                                                      *WorkingDirectory,
	                                                      WritePipe,
	                                                      ReadPipe);

	if (!ProcHandle.IsValid())
	{
		FString DialogText = FString::Printf(TEXT("%s failed to launch!"), *ProgramName);
		UE_LOG(LogUnrealSharpProcHelper, Error, TEXT("%s"), *DialogText);
		
		FMessageDialog::Open(EAppMsgType::Ok, FText::FromString(DialogText));
		return false;
	}
	
	while (FPlatformProcess::IsProcRunning(ProcHandle))
	{
		Output += FPlatformProcess::ReadPipe(ReadPipe);
	}
	
	FPlatformProcess::GetProcReturnCode(ProcHandle, &OutReturnCode);
	FPlatformProcess::CloseProc(ProcHandle);
	FPlatformProcess::ClosePipe(ReadPipe, WritePipe);

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

bool FCSProcHelper::InvokeUnrealSharpBuildTool(EBuildAction BuildAction, EDotNetBuildConfiguration* BuildConfiguration, const FString* InOutputDirectory)
{
	FName BuildActionCommand = StaticEnum<EBuildAction>()->GetNameByValue(BuildAction);
	FString PluginFolder = FPaths::ConvertRelativePathToFull(IPluginManager::Get().FindPlugin(UE_PLUGIN_NAME)->GetBaseDir());
	FString DotNetPath = GetDotNetExecutablePath();
	
	FString Args;
	Args += FString::Printf(TEXT("\"%s\""), *GetUnrealSharpBuildToolPath());
	Args += FString::Printf(TEXT(" --Action %s"), *BuildActionCommand.ToString());
	Args += FString::Printf(TEXT(" --EngineDirectory \"%s\""), *FPaths::ConvertRelativePathToFull(FPaths::EngineDir()));
	Args += FString::Printf(TEXT(" --ProjectDirectory \"%s\""), *FPaths::ConvertRelativePathToFull(FPaths::ProjectDir()));
	Args += FString::Printf(TEXT(" --ProjectName %s"), FApp::GetProjectName());
	Args += FString::Printf(TEXT(" --PluginDirectory \"%s\""), *PluginFolder);
	Args += FString::Printf(TEXT(" --DotNetPath \"%s\""), *DotNetPath);

	if (BuildConfiguration)
	{
		FText BuildConfigurationString = StaticEnum<EDotNetBuildConfiguration>()->GetDisplayNameTextByValue(static_cast<int64>(*BuildConfiguration));
		Args += FString::Printf(TEXT(" --BuildConfig %s"), *BuildConfigurationString.ToString());
	}
	
	int32 ReturnCode = 0;
	FString Output;
	FString WorkingDirectory = GetAssembliesPath();
	return InvokeCommand(DotNetPath, Args, ReturnCode, Output, &WorkingDirectory);
}

bool FCSProcHelper::Clean()
{
	return InvokeUnrealSharpBuildTool(EBuildAction::Clean);
}
 
bool FCSProcHelper::GenerateProject()
{
	return InvokeUnrealSharpBuildTool(EBuildAction::GenerateProject);
}

FString FCSProcHelper::GetLatestHostFxrPath()
{
	FString DotNetRoot = GetDotNetDirectory();
	FString HostFxrRoot = FPaths::Combine(DotNetRoot, "host", "fxr");

	TArray<FString> Folders;
	IFileManager::Get().FindFiles(Folders, *(HostFxrRoot / "*"), true, true);
	
	FString HighestVersion = "0.0.0";
	for (const FString& Folder : Folders)
	{
		if (Folder > HighestVersion)
		{
			HighestVersion = Folder;
		}
	}

	if (HighestVersion == "0.0.0")
	{
		UE_LOG(LogUnrealSharpProcHelper, Error, TEXT("Failed to find hostfxr version in %s"), *HostFxrRoot);
		return "";
	}
	
	return FPaths::Combine(HostFxrRoot, HighestVersion, HOSTFXR_WINDOWS);
}

FString FCSProcHelper::GetRuntimeHostPath()
{
#if WITH_EDITOR
	return GetLatestHostFxrPath();
#else
	return FPaths::Combine(GetAssembliesPath(), HOSTFXR_WINDOWS);
#endif
}

FString FCSProcHelper::GetAssembliesPath()
{
#if WITH_EDITOR
	return FPaths::Combine(GetPluginDirectory(), "Binaries", "Managed");
#else
	return GetUserAssemblyDirectory();
#endif
}

FString FCSProcHelper::GetUnrealSharpLibraryPath()
{
	return GetAssembliesPath() / "UnrealSharp.Plugins.dll";
}

FString FCSProcHelper::GetRuntimeConfigPath()
{
	return GetAssembliesPath() / "UnrealSharp.runtimeconfig.json";
}

FString FCSProcHelper::GetUserAssemblyDirectory()
{
	return FPaths::Combine(FPaths::ProjectDir(), "Binaries", "Managed");
}

FString FCSProcHelper::GetUserAssemblyPath()
{
	return FPaths::Combine(GetUserAssemblyDirectory(), GetUserManagedProjectName() + ".dll");
}

FString FCSProcHelper::GetManagedSourcePath()
{
	return FPaths::Combine(GetPluginDirectory(), "Managed");
}

FString FCSProcHelper::GetUnrealSharpBuildToolPath()
{
	return FPaths::ConvertRelativePathToFull(GetAssembliesPath() / "UnrealSharpBuildTool.dll");
}

FString FCSProcHelper::GetDotNetDirectory()
{
#if WITH_EDITOR
	const FString PathVariable = FPlatformMisc::GetEnvironmentVariable(TEXT("PATH"));
		
	TArray<FString> Paths;
	PathVariable.ParseIntoArray(Paths, FPlatformMisc::GetPathVarDelimiter());

	FString PathDotnet = "Program Files\\dotnet\\";
	for (FString& Path : Paths)
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
#else
	return GetAssembliesPath();
#endif
	return "";
}

FString FCSProcHelper::GetDotNetExecutablePath()
{
	return GetDotNetDirectory() + "dotnet.exe";
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

FString FCSProcHelper::GetScriptFolderDirectory()
{
	return FPaths::ProjectDir() / "Script";
}

FString FCSProcHelper::GetUserManagedProjectName()
{
	return FString::Printf(TEXT("Managed%s"), FApp::GetProjectName());
}

bool FCSProcHelper::BuildBindings(FString* OutputPath)
{
	int32 ReturnCode = 0;
	
	FString Arguments;
	Arguments += TEXT("publish");
	
	FString FullOutputPath = OutputPath ? *OutputPath : FPaths::ConvertRelativePathToFull(GetAssembliesPath());
	FString UnrealSharpDirectory = GetUnrealSharpDirectory();
	
	Arguments += FString::Printf(TEXT(" -p:PublishDir=\"%s\""), *FullOutputPath);

	FString Output;
	return InvokeCommand(GetDotNetExecutablePath(), Arguments, ReturnCode, Output, &UnrealSharpDirectory);
}
