#pragma once

namespace UnrealSharp::Paths
{
	UNREALSHARPUTILITIES_API FString GetDotNetDirectory();
	UNREALSHARPUTILITIES_API FString GetDotNetExecutablePath();
	UNREALSHARPUTILITIES_API FString GetLatestHostFxrPath();
	UNREALSHARPUTILITIES_API FString GetRuntimeHostPath();
	UNREALSHARPUTILITIES_API FString GetRuntimeConfigPath();
		
	UNREALSHARPUTILITIES_API FString& GetPluginDirectory();
	UNREALSHARPUTILITIES_API FString GetUnrealSharpDirectory();
	UNREALSHARPUTILITIES_API FString GetPluginAssembliesPath();
	UNREALSHARPUTILITIES_API FString GetUnrealSharpPluginsPath();
	UNREALSHARPUTILITIES_API FString GetUnrealSharpBuildToolPath();
	UNREALSHARPUTILITIES_API FString GetUserAssemblyDirectory();
	UNREALSHARPUTILITIES_API FString GetUnrealSharpMetadataPath();
	UNREALSHARPUTILITIES_API FString GetGeneratedClassesDirectory();
	UNREALSHARPUTILITIES_API const FString& GetScriptFolderDirectory();
	UNREALSHARPUTILITIES_API const FString& GetPluginsDirectory();
	UNREALSHARPUTILITIES_API const FString& GetProjectGlueFolderPath();
	UNREALSHARPUTILITIES_API FString GetPluginGlueFolderPath(const FString& PluginName);
	UNREALSHARPUTILITIES_API FString GetPathToManagedSolution();
}
