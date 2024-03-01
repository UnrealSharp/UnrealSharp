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

#define HOSTFXR_VERSION "8.0.1"
#define HOSTFXR_WINDOWS "hostfxr.dll"
#define HOSTFXR_MAC "libhostfxr.dylib"
#define HOSTFXR_LINUX "libhostfxr.so"
#define DOTNET_VERSION "net8.0"

class UNREALSHARPPROCHELPER_API FCSProcHelper final
{
public:
	
	static bool InvokeCommand(const FString& ProgramPath, const FString& Arguments, int32& OutReturnCode, FString& Output, FString* InWorkingDirectory = nullptr);
	static bool InvokeUnrealSharpBuildTool(EBuildAction BuildAction, const FString* BuildConfiguration = nullptr);

	static bool Build(const FString& BuildConfiguration);
	static bool Rebuild(const FString& BuildConfiguration);
	
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

	static bool BuildBindings(const FString& BuildConfiguration);

	static FString GetDotNetDirectory();
	static FString GetDotNetExecutablePath();

	static FString UserManagedProjectName;
	static FString PluginDirectory;
	static FString UnrealSharpDirectory;
	static FString GeneratedClassesDirectory;
	static FString ScriptFolderDirectory;
	
};
