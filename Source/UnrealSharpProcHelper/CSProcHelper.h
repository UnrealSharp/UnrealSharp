#pragma once

UENUM()
enum EBuildAction
{
	Build,
	Clean,
	GenerateProject,
	GenerateSolution,
	Rebuild,
	Weave,
	BuildWeave,
};

UENUM()
enum class EDotNetBuildConfiguration : uint64
{
	Debug,
	Release,
	Publish,
};

#define HOSTFXR_WINDOWS "hostfxr.dll"
#define HOSTFXR_MAC "libhostfxr.dylib"
#define HOSTFXR_LINUX "libhostfxr.so"
#define DOTNET_MAJOR_VERSION "8.0.0"

class UNREALSHARPPROCHELPER_API FCSProcHelper final
{
public:
	
	static bool InvokeCommand(const FString& ProgramPath, const FString& Arguments, int32& OutReturnCode, FString& Output, const FString* InWorkingDirectory = nullptr);
	static bool InvokeUnrealSharpBuildTool(EBuildAction BuildAction, EDotNetBuildConfiguration* BuildConfiguration = nullptr, const TMap<FString, FString>& AdditionalArguments = TMap<FString, FString>());
	
	static bool Clean();
	static bool GenerateProject();
	
	static FString GetRuntimeConfigPath();
	
	static FString GetAssembliesPath();
	static FString GetUnrealSharpLibraryPath();
	static FString GetUnrealSharpBuildToolPath();

	// Path to the directory where we store the user's assembly after it has been processed by the weaver.
	static FString GetUserAssemblyDirectory();

	//Path to all use assemblies in the Binaries/managed directory
	static void GetAllUserAssemblyPaths(TArray<FString>& AssemblyPaths);

	// Path to all project directories in /Script
	static void GetAllProjectPaths(TArray<FString>& ProjectPaths);

	// Path to the .NET runtime root. Only really works in editor, since players don't have the .NET runtime.
	static FString GetDotNetDirectory();

	// Path to the .NET executable. Only really works in editor, since players don't have the .NET runtime.
	static FString GetDotNetExecutablePath();

	// Path to the UnrealSharp plugin directory.
	static FString& GetPluginDirectory();

	// Path to the UnrealSharp bindings directory.
	static FString GetUnrealSharpDirectory();

	// Path to the directory where we store classes we have generated from C# > C++.
	static FString GetGeneratedClassesDirectory();

	// Path to the current project's script directory
	static FString& GetScriptFolderDirectory();

	// Get the name of the current managed version of the project
	static FString GetUserManagedProjectName();

	// Path to the latest installed hostfxr version
	static FString GetLatestHostFxrPath();

	// Path to the runtime host. This is different in editor/builds.
	static FString GetRuntimeHostPath();

	// Path to the C# solution file.
	static FString GetPathToSolution();
	
};
