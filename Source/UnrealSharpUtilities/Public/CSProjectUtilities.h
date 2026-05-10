#pragma once

namespace UnrealSharp::Project
{
	UNREALSHARPUTILITIES_API void GetProjectNamesByLoadOrder(TArray<FString>& UserProjectNames, bool bIncludeGlue = false);
	UNREALSHARPUTILITIES_API void GetAssemblyPathsByLoadOrder(TArray<FString>& AssemblyPaths, bool bIncludeGlue = false);
	UNREALSHARPUTILITIES_API void GetAllProjectPaths(TArray<FString>& ProjectPaths, bool bIncludeProjectGlue = false);

	UNREALSHARPUTILITIES_API FString GetUserManagedProjectName();
	UNREALSHARPUTILITIES_API FString AppendGlueSuffix(const FString& FileName);
}
