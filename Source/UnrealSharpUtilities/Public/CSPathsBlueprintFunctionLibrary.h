#pragma once

#include "CSPathsBlueprintFunctionLibrary.generated.h"

UCLASS()
class UCSPathsBlueprintFunctionLibrary : public UBlueprintFunctionLibrary
{
	GENERATED_BODY()
public:
	UFUNCTION(meta = (ScriptMethod))
	static FString GetRuntimeConfigPath();

	UFUNCTION(meta = (ScriptMethod))
	static FString GetPluginAssembliesPath();

	UFUNCTION(meta = (ScriptMethod))
	static FString GetUnrealSharpPluginsPath();

	UFUNCTION(meta = (ScriptMethod))
	static FString GetUnrealSharpBuildToolPath();

	UFUNCTION(meta = (ScriptMethod))
	static FString GetUserAssemblyDirectory();

	UFUNCTION(meta = (ScriptMethod))
	static FString GetUnrealSharpMetadataPath();

	UFUNCTION(meta = (ScriptMethod))
	static FString GetDotNetDirectory();

	UFUNCTION(meta = (ScriptMethod))
	static FString GetDotNetExecutablePath();

	UFUNCTION(meta = (ScriptMethod))
	static FString& GetPluginDirectory();

	UFUNCTION(meta = (ScriptMethod))
	static FString GetUnrealSharpDirectory();

	UFUNCTION(meta = (ScriptMethod))
	static FString GetGeneratedClassesDirectory();

	UFUNCTION(meta = (ScriptMethod))
	static const FString& GetScriptFolderDirectory();

	UFUNCTION(meta = (ScriptMethod))
	static const FString& GetPluginsDirectory();

	UFUNCTION(meta = (ScriptMethod))
	static const FString& GetProjectGlueFolderPath();

	UFUNCTION(meta = (ScriptMethod))
	static FString GetPluginGlueFolderPath(const FString& PluginName);

	UFUNCTION(meta = (ScriptMethod))
	static FString GetLatestHostFxrPath();

	UFUNCTION(meta = (ScriptMethod))
	static FString GetRuntimeHostPath();

	UFUNCTION(meta = (ScriptMethod))
	static FString GetPathToManagedSolution();

	UFUNCTION(meta = (ScriptMethod))
	static FString AppendGlueSuffix(const FString& FileName);

	UFUNCTION(meta = (ScriptMethod))
	static FString GetUserManagedProjectName();

	UFUNCTION(meta = (ScriptMethod))
	static void GetProjectNamesByLoadOrder(TArray<FString>& UserProjectNames, bool bIncludeGlue = false);

	UFUNCTION(meta = (ScriptMethod))
	static void GetAssemblyPathsByLoadOrder(TArray<FString>& AssemblyPaths, bool bIncludeGlue = false);

	UFUNCTION(meta = (ScriptMethod))
	static void GetAllProjectPaths(TArray<FString>& ProjectPaths, bool bIncludeProjectGlue = false);
};
