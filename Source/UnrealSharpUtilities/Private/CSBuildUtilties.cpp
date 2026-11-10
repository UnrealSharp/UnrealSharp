
#if WITH_EDITOR

#include "CSBuildUtilties.h"
#include "CSBuildActionUtilities.h"
#include "CSPathsUtilities.h"
#include "CSProcessUtilities.h"
#include "CSUnrealSharpUtilitiesSettings.h"
#include "CoreGlobals.h"
#include "HAL/PlatformMisc.h"
#include "IUATHelperModule.h"
#include "Interfaces/IPluginManager.h"
#include "Misc/MonitoredProcess.h"
#include "Misc/Guid.h"
#include "Styling/SlateStyleRegistry.h"

#define LOCTEXT_NAMESPACE "UnrealSharpBuildUtilities"

namespace UATEnvVarNames
{
	// Matches AutomationTool.EnvVarNames in AutomationUtils/CommandEnvironment.cs.
	constexpr const TCHAR* LogFolder = TEXT("uebp_LogFolder");
	constexpr const TCHAR* FinalLogFolder = TEXT("uebp_FinalLogFolder");
	constexpr const TCHAR* DisableStartupMutex = TEXT("uebp_UATMutexNoWait");
	constexpr const TCHAR* IsChildInstance = TEXT("uebp_UATChildInstance");
}

bool UnrealSharp::Build::InvokeUnrealSharpAutomation(const FString& BuildAction, const TMap<FString, FString>* ActionArgs, const FCSCommandError& OnError)
{
	FString Arguments;
	BuildArguments(BuildAction, ActionArgs, Arguments);

	int32 ReturnCode = 0;
	FString Output;
	return Process::InvokeCommand(FSerializedUATProcess::GetUATPath(), Arguments, ReturnCode, Output, nullptr, OnError);
}

void UnrealSharp::Build::InvokeUnrealSharpAutomation_Async(const FString& BuildAction, const FText& BuildActionDisplayName, const TMap<FString, FString>* ActionArgs, const IUATHelperModule::UatTaskResultCallack& ResultCallback)
{
	if (!IsValid(GEditor))
	{
		if (InvokeUnrealSharpAutomation(BuildAction, ActionArgs))
		{
			 ResultCallback(TEXT("Completed"), 0.0);
		}
		else
		{
			 ResultCallback(TEXT("Failed"), -1.0);
		}
		
		return;
	}
	
	FString Arguments;
	BuildArguments(BuildAction, ActionArgs, Arguments);
	
#if PLATFORM_WINDOWS
	FText PlatformName = LOCTEXT("PlatformName_Windows", "Windows");
#elif PLATFORM_MAC
	FText PlatformName = LOCTEXT("PlatformName_Mac", "Mac");
#elif PLATFORM_LINUX
	FText PlatformName = LOCTEXT("PlatformName_Linux", "Linux");
#else
	FText PlatformName = LOCTEXT("PlatformName_Other", "Other OS");
#endif
	
	IUATHelperModule::Get().CreateUatTask(Arguments, 
		PlatformName, 
		BuildActionDisplayName,
	BuildActionDisplayName, 
	GetBuildActionIcon(),
	nullptr, 
	ResultCallback);
}

bool UnrealSharp::Build::BuildUserSolution(const FCSCommandError& OnError)
{
	FString SolutionDirectory = FPaths::ConvertRelativePathToFull(Paths::GetScriptFolderDirectory());
	
	TMap<FString, FString> Arguments;
	Arguments.Add(TEXT("OutputPath"), Paths::MakeQuotedPath(Paths::GetUserAssemblyDirectory()));
	Arguments.Add(TEXT("TargetConfiguration"), LexToString(FApp::GetBuildConfiguration()));
	
	if (!GetDefault<UCSUnrealSharpUtilitiesSettings>()->bShowBuildWarnings)
	{
		Arguments.Add(TEXT("clp"), TEXT("ErrorsOnly"));
	}

	return InvokeUnrealSharpAutomation(BuildAction::BuildUserSolution, &Arguments, OnError);
}

void UnrealSharp::Build::BuildArguments(const FString& BuildAction, const TMap<FString, FString>* ActionArgs, FString& OutArgs)
{
	const FString PluginFolder = FPaths::ConvertRelativePathToFull(IPluginManager::Get().FindPlugin(UE_PLUGIN_NAME)->GetBaseDir());
	const FString ProjectFolder = FPaths::ConvertRelativePathToFull(FPaths::ProjectDir());
	const FString EngineFolder = FPaths::ConvertRelativePathToFull(FPaths::EngineDir());
	const FString ProjectFile = FPaths::ConvertRelativePathToFull(FPaths::GetProjectFilePath());
	
	OutArgs.Reset();
	OutArgs += BuildAction;
	const FString ParentLogFolder = FPlatformMisc::GetEnvironmentVariable(UATEnvVarNames::LogFolder);
	if (IsRunningCommandlet() && !ParentLogFolder.IsEmpty())
	{
		// Follow CommandUtils.RunUAT's child-instance/log isolation in AutomationUtils/ProcessUtils.cs.
		// UAT parses NAME=VALUE arguments before its startup mutex check (Program.cs),
		// so these overrides affect only the child process. Reuse the parent's compiled tools.
		const FString ChildLogFolder = FPaths::Combine(ParentLogFolder, TEXT("UnrealSharp"), FGuid::NewGuid().ToString(EGuidFormats::Digits));
		OutArgs += FString::Printf(TEXT(" -nocompileuat -nocompile %s=1 %s=1"),
			UATEnvVarNames::DisableStartupMutex, UATEnvVarNames::IsChildInstance);
		OutArgs += FString::Printf(TEXT(" %s=\"%s\" %s=\"%s\""),
			UATEnvVarNames::LogFolder, *ChildLogFolder,
			UATEnvVarNames::FinalLogFolder, *ChildLogFolder);
	}
	if (FPaths::IsUnderDirectory(PluginFolder, ProjectFolder))
	{
		// Use the same discovery and build-record root as -ScriptsForProject.
		// Reading the legacy Build/Scripts root too can load duplicate records.
		OutArgs += FString::Printf(TEXT(" -ScriptDir=\"%s\""), *ProjectFolder);
	}
	else if (!FPaths::IsUnderDirectory(PluginFolder, EngineFolder))
	{
		// External plugins are not covered by the default engine/project roots.
		OutArgs += FString::Printf(TEXT(" -ScriptDir=\"%s\""), *FPaths::Combine(PluginFolder, TEXT("Build"), TEXT("Scripts")));
	}
	// -Project selects the command target; -ScriptsForProject also scopes UAT script discovery.
	OutArgs += FString::Printf(TEXT(" -ScriptsForProject=\"%s\" -Project=\"%s\""), *ProjectFile, *ProjectFile);
	
	if (ActionArgs && ActionArgs->Num() > 0)
	{
		for (const auto& [Key, Value] : *ActionArgs)
		{
			OutArgs += FString::Printf(TEXT(" -%s=%s"), *Key, *Value);
		}
	}
}

const FSlateBrush* UnrealSharp::Build::GetBuildActionIcon()
{
	const ISlateStyle* Style = FSlateStyleRegistry::FindSlateStyle("UnrealSharpStyle");
	return Style->GetBrush("UnrealSharp.Toolbar");
}

#endif

#undef LOCTEXT_NAMESPACE
