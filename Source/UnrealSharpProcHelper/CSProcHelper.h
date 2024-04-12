#pragma once

UENUM()
enum EBuildAction
{
	Build,
	Clean,
	GenerateProject,
	Rebuild,
	Weave,
};

UENUM()
enum class EDotNetBuildConfiguration : uint64
{
	Debug,
	Release,
	Publish,
};

#define HOSTFXR_VERSION "8.0.1"
#define HOSTFXR_WINDOWS "hostfxr.dll"
#define HOSTFXR_MAC "libhostfxr.dylib"
#define HOSTFXR_LINUX "libhostfxr.so"
#define DOTNET_VERSION "net8.0"

class UNREALSHARPPROCHELPER_API FCSProcHelper final
{
public:
	
	static bool InvokeCommand(const FString& ProgramPath, const FString& Arguments, int32& OutReturnCode, FString& Output, FString* InWorkingDirectory = nullptr);
	static bool InvokeUnrealSharpBuildTool(EBuildAction BuildAction, EDotNetBuildConfiguration* BuildConfiguration = nullptr, const FString* OutputDirectory = nullptr);
	
	static bool Clean();
	static bool GenerateProject();

	static FString GetRuntimeHostPath();
	static FString GetAssembliesPath();
	static FString GetUnrealSharpLibraryPath();
	static FString GetRuntimeConfigPath();
	static FString GetUserAssemblyDirectory();
	static FString GetUserAssemblyPath();
	static FString GetManagedSourcePath();
	static FString GetUnrealSharpBuildToolPath();

	static bool BuildBindings(FString* OutputPath = nullptr);

	static FString GetDotNetDirectory();
	static FString GetDotNetExecutablePath();

	static FString& GetPluginDirectory();
	static FString GetUnrealSharpDirectory();
	static FString GetGeneratedClassesDirectory();
	static FString GetScriptFolderDirectory();
	static FString GetUserManagedProjectName();

	static FString GetLatestHostFxrPath();
	
};
