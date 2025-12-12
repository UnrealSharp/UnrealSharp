#pragma once

#include "CSProcUtilities.generated.h"

const FString BUILD_ACTION_GENERATE_PROJECT = TEXT("GenerateProject");
const FString BUILD_ACTION_GENERATE_SOLUTION = TEXT("GenerateSolution");
const FString BUILD_ACTION_BUILD_EMIT_LOAD_ORDER = TEXT("BuildEmitLoadOrder");
const FString BUILD_ACTION_PACKAGE_PROJECT = TEXT("PackageProject");

#define HOSTFXR_WINDOWS "hostfxr.dll"
#define HOSTFXR_MAC "libhostfxr.dylib"
#define HOSTFXR_LINUX "libhostfxr.so"

#define STRINGIFY_IMPL(x) #x
#define STRINGIFY(x) STRINGIFY_IMPL(x)

#define DOTNET_MAJOR_VERSION_INT 9
#define DOTNET_MAJOR_VERSION STRINGIFY(DOTNET_MAJOR_VERSION_INT) ".0.0"
#define DOTNET_DISPLAY_NAME "net" STRINGIFY(DOTNET_MAJOR_VERSION_INT) ".0"

UCLASS()
class UNREALSHARPPROCHELPER_API UCSProcUtilities : public UObject
{
	GENERATED_BODY()
	
public:
	static bool InvokeCommand(const FString& ProgramPath, const FString& Arguments, int32& OutReturnCode, FString& Output, const FString* InWorkingDirectory = nullptr);
	static bool InvokeUnrealSharpBuildTool(const FString& BuildAction, const TMap<FString, FString>& AdditionalArguments = TMap<FString, FString>());
	
	static bool InvokeDotNet(const FString& Arguments, const FString* InWorkingDirectory = nullptr)
	{
		FString Output;
		int32 OutReturnCode = 0;
		return InvokeCommand(GetDotNetExecutablePath(), Arguments, OutReturnCode, Output, InWorkingDirectory);
	}
	
	static bool InvokeDotNetBuild(const FString& RootFolder, const FString& AdditionalArguments = FString())
	{
		FString Args = FString::Printf(TEXT("build \"%s\" %s"), *RootFolder, *AdditionalArguments);
		return InvokeDotNet(Args, nullptr);
	}
	
	static bool InvokeDotNetBuild()
	{
		return InvokeDotNetBuild(GetScriptFolderDirectory());
	}

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

	// Path to file with UnrealSharp metadata
	UFUNCTION(meta = (ScriptMethod))
	static FString GetUnrealSharpMetadataPath();

	// Gets the project names in the order they should be loaded.
	UFUNCTION(meta = (ScriptMethod))
	static void GetProjectNamesByLoadOrder(TArray<FString>& UserProjectNames, bool bIncludeGlue = false);

	// Same as GetProjectNamesByLoadOrder, but returns the paths to the assemblies instead.
	UFUNCTION(meta = (ScriptMethod))
	static void GetAssemblyPathsByLoadOrder(TArray<FString>& AssemblyPaths, bool bIncludeGlue = false);

	// Gets all the project paths in the /Scripts directory.
	UFUNCTION(meta = (ScriptMethod))
	static void GetAllProjectPaths(TArray<FString>& ProjectPaths, bool bIncludeProjectGlue = false);

	// Path to the .NET runtime root. Only really works in editor, since players don't have the .NET runtime.
	UFUNCTION(meta = (ScriptMethod))
	static FString GetDotNetDirectory();

	// Path to the .NET executable. Only really works in editor, since players don't have the .NET runtime.
	UFUNCTION(meta = (ScriptMethod))
	static FString GetDotNetExecutablePath();

	// Path to the UnrealSharp plugin directory.
	UFUNCTION(meta = (ScriptMethod))
	static FString& GetPluginDirectory();

	// Path to the UnrealSharp bindings directory.
	UFUNCTION(meta = (ScriptMethod))
	static FString GetUnrealSharpDirectory();

	// Path to the directory where we store classes we have generated from C# > C++.
	UFUNCTION(meta = (ScriptMethod))
	static FString GetGeneratedClassesDirectory();

	// Path to the current project's script directory
	UFUNCTION(meta = (ScriptMethod))
	static const FString& GetScriptFolderDirectory();

	UFUNCTION(meta = (ScriptMethod))
    static const FString& GetPluginsDirectory();

	// Path to the current project's glue directory
	UFUNCTION(meta = (ScriptMethod))
	static const FString& GetProjectGlueFolderPath();

	// Get the name of the current managed version of the project
	UFUNCTION(meta = (ScriptMethod))
	static FString GetUserManagedProjectName();

	// Path to the latest installed hostfxr version
	UFUNCTION(meta = (ScriptMethod))
	static FString GetLatestHostFxrPath();

	// Path to the runtime host. This is different in editor/builds.
	UFUNCTION(meta = (ScriptMethod))
	static FString GetRuntimeHostPath();

	// Path to the C# solution file.
	UFUNCTION(meta = (ScriptMethod))
	static FString GetPathToManagedSolution();

private:
	static FString& GetManagedBinaries();
};
