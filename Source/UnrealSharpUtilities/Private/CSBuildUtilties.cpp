
#if WITH_EDITOR

#include "CSBuildUtilties.h"
#include "CSBuildActionUtilities.h"
#include "CSPathsUtilities.h"
#include "CSProcessUtilities.h"
#include "CSUnrealSharpUtilitiesSettings.h"
#include "IUATHelperModule.h"
#include "Interfaces/IPluginManager.h"
#include "Misc/MonitoredProcess.h"
#include "Styling/SlateStyleRegistry.h"

#define LOCTEXT_NAMESPACE "UnrealSharpBuildUtilities"

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
	TMap<FString, FString> Arguments;
	Arguments.Add(TEXT("OutputPath"), Paths::MakeQuotedPath(Paths::GetUserAssemblyDirectory()));
	
	if (!GetDefault<UCSUnrealSharpUtilitiesSettings>()->bShowBuildWarnings)
	{
		Arguments.Add(TEXT("clp"), TEXT("ErrorsOnly"));
	}

	return InvokeUnrealSharpAutomation(BuildAction::BuildEmitLoadOrder, &Arguments, OnError);
}

void UnrealSharp::Build::BuildArguments(const FString& BuildAction, const TMap<FString, FString>* ActionArgs, FString& OutArgs)
{
	const FString PluginFolder = FPaths::ConvertRelativePathToFull(IPluginManager::Get().FindPlugin(UE_PLUGIN_NAME)->GetBaseDir());
	const FString DotNetPath = Paths::GetDotNetExecutablePath();
	
	OutArgs.Reset();
	OutArgs += BuildAction;
	OutArgs += FString::Printf(TEXT(" -ScriptDir=\"%s\""), *FPaths::Combine(PluginFolder, TEXT("Build"), TEXT("Scripts")));
	OutArgs += FString::Printf(TEXT(" -Project=\"%s\""), *FPaths::ConvertRelativePathToFull(FPaths::GetProjectFilePath()));
	
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
