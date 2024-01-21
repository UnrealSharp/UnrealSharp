#pragma once

#include <coreclr_delegates.h>
#include <hostfxr.h>
#include "CSAssembly.h"
#include "CSManagedCallbacksCache.h"

class FUSScriptEngine;
class FUSTypeFactory;
struct FTypeReferenceMetaData;
struct FGCHandle;
class FUSManagedObject;
struct FCSAssembly;

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

using FInitializeRuntimeHost = bool (*)(const TCHAR*, FCSManagedPluginCallbacks*, FCSManagedCallbacks::FManagedCallbacks*, const void*);


class CSHARPFORUE_API FCSManager : public FUObjectArray::FUObjectDeleteListener
{
	
public:

	static FCSManager& Get()
	{
		static FCSManager Instance;
		return Instance;
	}

	void InitializeUnrealSharp();

	static UPackage* GetUnrealSharpPackage();
	static FString GetRuntimeHostPath();;
	static FString GetAssembliesPath();
	static FString GetUnrealSharpLibraryPath();
	static FString GetRuntimeConfigPath();
	static FString GetUserAssemblyDirectory();
	static FString GetUserAssemblyPath();
	static FString GetManagedSourcePath();
	static FString GetUnrealSharpBuildToolPath();

	TSharedPtr<FCSAssembly> LoadPlugin(const FString& AssemblyPath);
	bool UnloadPlugin(const FString& AssemblyName);

	FGCHandle CreateNewManagedObject(UObject* Object, UClass* Class);
	FGCHandle CreateNewManagedObject(UObject* Object, uint8* TypeHandle);
	
	FGCHandle FindManagedObject(UObject* Object);
	
	void RemoveManagedObject(UObject* Object);

	uint8* GetTypeHandle(const FString& AssemblyName, const FString& Namespace, const FString& TypeName);
	uint8* GetTypeHandle(const FTypeReferenceMetaData& TypeMetaData);

	static bool InvokeUnrealSharpBuildTool(EBuildAction BuildAction);
	static bool InvokeCommand(const FString& ProgramPath, const FString& Arguments, int32& OutReturnCode, FString& Output, FString* WorkingDirectory = nullptr);

	bool LoadUserAssembly();

	TMap<FName, TSharedPtr<FCSAssembly>> LoadedPlugins;
	TMap<UObject*, FGCHandle> UnmanagedToManagedMap;

	static FString PluginDirectory;
	static FString UserManagedProjectName;
	static FString UnrealSharpDirectory;
	static FString GeneratedClassesDirectory;
	static FString ScriptFolderDirectory;
	static FString BatchFilesDirectory;
	
	static inline FCSManagedPluginCallbacks ManagedPluginsCallbacks;

private:
	
	static FUSScriptEngine* UnrealSharpScriptEngine;
	static UPackage* UnrealSharpPackage;
	
	bool LoadRuntimeHost();
	bool InitializeBindings();
	
	load_assembly_and_get_function_pointer_fn InitializeRuntimeHost() const;

	// Begin FUObjectArray::FUObjectDeleteListener Api
	virtual void NotifyUObjectDeleted(const UObjectBase *Object, int32 Index) override;
	virtual void OnUObjectArrayShutdown() override;
	// End FUObjectArray::FUObjectDeleteListener Api
	
	static bool BuildBindings();
	static bool BuildPrograms();

	static FString GetDotNetDirectory();
	static FString GetDotNetExecutablePath();

	static bool Build();
	static bool Clean();
	static bool Rebuild();
	static bool GenerateProject();
	
	//.NET Core Host API
	hostfxr_initialize_for_dotnet_command_line_fn Hostfxr_Initialize_For_Dotnet_Command_Line = nullptr;
	hostfxr_initialize_for_runtime_config_fn Hostfxr_Initialize_For_Runtime_Config = nullptr;
	hostfxr_get_runtime_delegate_fn Hostfxr_Get_Runtime_Delegate = nullptr;
	hostfxr_close_fn Hostfxr_Close = nullptr;

	void* RuntimeHost = nullptr;
	void* UnrealSharpLibraryDLL = nullptr;
	void* UserScriptsDLL = nullptr;
	//End
};
