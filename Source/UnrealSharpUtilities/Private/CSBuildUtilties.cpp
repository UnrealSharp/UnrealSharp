#include "CSBuildUtilties.h"

#include "CSBuildActionUtilities.h"
#include "CSPathsUtilities.h"
#include "CSProcessUtilities.h"
#include "CSUnrealSharpUtilitiesSettings.h"
#include "Interfaces/IPluginManager.h"
#include "Misc/MonitoredProcess.h"

bool UnrealSharp::Build::InvokeUnrealSharpAutomation(const FString& BuildAction, const TMap<FString, FString>* ActionArgs, const FCSCommandError& OnError)
{
	const FString PluginFolder = FPaths::ConvertRelativePathToFull(IPluginManager::Get().FindPlugin(UE_PLUGIN_NAME)->GetBaseDir());
	const FString DotNetPath = Paths::GetDotNetExecutablePath();

	FString Args;
	Args += FString::Printf(TEXT("%s"), *BuildAction);
	Args += FString::Printf(TEXT(" -ScriptDir=\"%s\""), *FPaths::Combine(PluginFolder, TEXT("Build"), TEXT("Scripts")));
	Args += FString::Printf(TEXT(" -Project=\"%s\""), *FPaths::ConvertRelativePathToFull(FPaths::GetProjectFilePath()));
	
	if (ActionArgs && ActionArgs->Num() > 0)
	{
		for (const auto& [Key, Value] : *ActionArgs)
		{
			Args += FString::Printf(TEXT(" -%s=%s"), *Key, *Value);
		}
	}

	int32 ReturnCode = 0;
	FString Output;
	return Process::InvokeCommand(FSerializedUATProcess::GetUATPath(), Args, ReturnCode, Output, nullptr, OnError);
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
