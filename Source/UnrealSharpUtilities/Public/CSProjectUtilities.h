#pragma once

struct FCSLoadOrderManifest
{
	FString Name;
	int32 Priority = 0;
	bool bCollectible = true;
	TArray<FString> AssemblyPaths;
	
	bool ContainsAssembly(const FString& AssemblyName) const
	{
		bool bFound = false;
		for (const FString& Path : AssemblyPaths)
		{
			if (FPaths::GetCleanFilename(Path) != AssemblyName && FPaths::GetBaseFilename(Path) != AssemblyName)
			{
				continue;
			}
			
			bFound = true;
			break;
		}
		
		return bFound;
	}
};

namespace UnrealSharp::Project
{
	UNREALSHARPUTILITIES_API void DiscoverLoadOrderManifests(TArray<FCSLoadOrderManifest>& OutManifests);
	UNREALSHARPUTILITIES_API bool IsAssemblyInAnyManifest(const FString& AssemblyName);
	UNREALSHARPUTILITIES_API void GetAllProjectPaths(TArray<FString>& ProjectPaths);
	UNREALSHARPUTILITIES_API FString GetUserManagedProjectName();
}
