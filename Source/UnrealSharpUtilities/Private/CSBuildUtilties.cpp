#include "CSBuildUtilties.h"

#include "CSBuildActionUtilities.h"
#include "CSPathsUtilities.h"
#include "CSProcessUtilities.h"
#include "CSUnrealSharpUtilitiesSettings.h"
#include "Interfaces/IPluginManager.h"

bool UnrealSharp::Build::InvokeUnrealSharpBuildTool(const FString& BuildAction, const TMap<FString, FString>* ActionArgs, const FCSCommandError& OnError)
{
	const FString PluginFolder = FPaths::ConvertRelativePathToFull(IPluginManager::Get().FindPlugin(UE_PLUGIN_NAME)->GetBaseDir());
	const FString DotNetPath = Paths::GetDotNetExecutablePath();

	FString Args;
	Args += FString::Printf(TEXT("\"%s\""), *FPaths::ConvertRelativePathToFull(Paths::GetUnrealSharpBuildToolPath()));
	Args += FString::Printf(TEXT(" --Action %s"), *BuildAction);
	Args += FString::Printf(TEXT(" --EngineDirectory \"%s\""), *FPaths::ConvertRelativePathToFull(FPaths::EngineDir()));
	Args += FString::Printf(TEXT(" --ProjectDirectory \"%s\""), *FPaths::ConvertRelativePathToFull(FPaths::ProjectDir()));
	Args += FString::Printf(TEXT(" --ProjectName %s"), FApp::GetProjectName());
	Args += FString::Printf(TEXT(" --PluginDirectory \"%s\""), *PluginFolder);
	Args += FString::Printf(TEXT(" --DotNetPath \"%s\""), *DotNetPath);

	if (ActionArgs && ActionArgs->Num() > 0)
	{
		Args += TEXT(" --ActionArgs");
		for (const auto& [Key, Value] : *ActionArgs)
		{
			Args += FString::Printf(TEXT(" %s=%s"), *Key, *Value);
		}
	}

	int32 ReturnCode = 0;
	FString Output;
	FString WorkingDirectory = Paths::GetPluginAssembliesPath();
	return Process::InvokeCommand(DotNetPath, Args, ReturnCode, Output, &WorkingDirectory, OnError);
}

bool UnrealSharp::Build::BuildUserSolution(const FCSCommandError& OnError)
{
	TMap<FString, FString> Arguments;
	Arguments.Add(TEXT("OutputPath"), Paths::GetUserAssemblyDirectory());
	
	if (!GetDefault<UCSUnrealSharpUtilitiesSettings>()->bShowBuildWarnings)
	{
		Arguments.Add(TEXT("clp"), TEXT("ErrorsOnly"));
	}

	return InvokeUnrealSharpBuildTool(BuildAction::BuildEmitLoadOrder, &Arguments, OnError);
}
