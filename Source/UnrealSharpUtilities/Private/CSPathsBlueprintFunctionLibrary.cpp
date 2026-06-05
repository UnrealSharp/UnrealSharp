#include "CSPathsBlueprintFunctionLibrary.h"

#include "CSDotnetUtilties.h"
#include "CSPathsUtilities.h"
#include "CSProjectUtilities.h"

FString UCSPathsBlueprintFunctionLibrary::GetRuntimeConfigPath()
{
	return UnrealSharp::DotNetUtilities::GetRuntimeConfigPath();
}

FString UCSPathsBlueprintFunctionLibrary::GetPluginAssembliesPath()
{
	return UnrealSharp::Paths::GetPluginAssembliesPath();
}

FString UCSPathsBlueprintFunctionLibrary::GetUnrealSharpPluginsPath()
{
	return UnrealSharp::Paths::GetUnrealSharpPluginsPath();
}

FString UCSPathsBlueprintFunctionLibrary::GetUnrealSharpBuildToolPath()
{
	return UnrealSharp::Paths::GetUnrealSharpBuildToolPath();
}

FString UCSPathsBlueprintFunctionLibrary::GetUserAssemblyDirectory()
{
	return UnrealSharp::Paths::GetUserAssemblyDirectory();
}

FString UCSPathsBlueprintFunctionLibrary::GetUnrealSharpMetadataPath()
{
	return UnrealSharp::Paths::GetUnrealSharpMetadataPath();
}

FString UCSPathsBlueprintFunctionLibrary::GetDotNetDirectory()
{
	return UnrealSharp::DotNetUtilities::GetDotNetDirectory();
}

FString UCSPathsBlueprintFunctionLibrary::GetDotNetExecutablePath()
{
	return UnrealSharp::DotNetUtilities::GetDotNetExecutablePath();
}

FString UCSPathsBlueprintFunctionLibrary::GetPluginDirectory()
{
	return UnrealSharp::Paths::GetPluginDirectory();
}

FString UCSPathsBlueprintFunctionLibrary::GetUnrealSharpDirectory()
{
	return UnrealSharp::Paths::GetUnrealSharpDirectory();
}

FString UCSPathsBlueprintFunctionLibrary::GetGeneratedClassesDirectory()
{
	return UnrealSharp::Paths::GetGeneratedClassesDirectory();
}

const FString& UCSPathsBlueprintFunctionLibrary::GetScriptFolderDirectory()
{
	return UnrealSharp::Paths::GetScriptFolderDirectory();
}

const FString& UCSPathsBlueprintFunctionLibrary::GetPluginsDirectory()
{
	return UnrealSharp::Paths::GetPluginsDirectory();
}

FString UCSPathsBlueprintFunctionLibrary::GetLatestHostFxrPath()
{ 
	return UnrealSharp::DotNetUtilities::GetLatestHostFxrPath();
}

FString UCSPathsBlueprintFunctionLibrary::GetRuntimeHostPath()
{
	return UnrealSharp::DotNetUtilities::GetRuntimeHostPath();
}

FString UCSPathsBlueprintFunctionLibrary::GetPathToManagedSolution()
{
	return UnrealSharp::Paths::GetPathToManagedSolution();
}

FString UCSPathsBlueprintFunctionLibrary::GetUserManagedProjectName()
{
	return UnrealSharp::Project::GetUserManagedProjectName();
}

void UCSPathsBlueprintFunctionLibrary::GetAllProjectPaths(TArray<FString>& ProjectPaths)
{
	UnrealSharp::Project::GetAllProjectPaths(ProjectPaths);
}
