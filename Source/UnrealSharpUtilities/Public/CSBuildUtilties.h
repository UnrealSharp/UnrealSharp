#pragma once
#include "CSProcessUtilities.h"

#if WITH_EDITOR
#include "IUATHelperModule.h"

namespace UnrealSharp::Build
{
	UNREALSHARPUTILITIES_API bool InvokeUnrealSharpAutomation(const FString& BuildAction, const TMap<FString, FString>* ActionArgs = nullptr, const FCSCommandError& OnError = {});
	UNREALSHARPUTILITIES_API void InvokeUnrealSharpAutomation_Async(const FString& BuildAction, const FText& BuildActionDisplayName, const TMap<FString, FString>* ActionArgs = nullptr, const IUATHelperModule::UatTaskResultCallack& ResultCallback = IUATHelperModule::UatTaskResultCallack());
	UNREALSHARPUTILITIES_API bool BuildUserSolution(const FCSCommandError& OnError = {});
	
	void BuildArguments(const FString& BuildAction, const TMap<FString, FString>* ActionArgs, FString& OutArgs);
	const FSlateBrush* GetBuildActionIcon();
}

#endif
