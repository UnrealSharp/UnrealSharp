#pragma once
#include "CSProcessUtilities.h"

namespace UnrealSharp::Build
{
	UNREALSHARPUTILITIES_API bool InvokeUnrealSharpBuildTool(const FString& BuildAction, const TMap<FString, FString>* ActionArgs = nullptr, const FCSCommandError& OnError = {});
	UNREALSHARPUTILITIES_API bool BuildUserSolution(const FCSCommandError& OnError = {});
}
