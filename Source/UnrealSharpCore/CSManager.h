#pragma once

#include <coreclr_delegates.h>
#include <hostfxr.h>
#include "CSAssembly.h"
#include "CSManagedCallbacksCache.h"
#include "CSManager.generated.h"

class FUnrealSharpCoreModule;
class UFunctionsExporter;
struct FCSNamespace;
struct FCSTypeReferenceMetaData;

struct FCSManagedPluginCallbacks
{
	using LoadPluginCallback = GCHandleIntPtr(__stdcall*)(const TCHAR*, bool);
	using UnloadPluginCallback = bool(__stdcall*)(const TCHAR*);
	
	LoadPluginCallback LoadPlugin = nullptr;
	UnloadPluginCallback UnloadPlugin = nullptr;
};

using FInitializeRuntimeHost = bool (*)(const TCHAR*, const TCHAR*, FCSManagedPluginCallbacks*, const void*, FCSManagedCallbacks::FManagedCallbacks*);

DECLARE_MULTICAST_DELEGATE_OneParam(FOnManagedAssemblyLoaded, const FName&);
DECLARE_MULTICAST_DELEGATE(FOnAssembliesReloaded);

DECLARE_MULTICAST_DELEGATE_OneParam(FCSClassEvent, UClass*);
DECLARE_MULTICAST_DELEGATE_OneParam(FCSStructEvent, UScriptStruct*);
DECLARE_MULTICAST_DELEGATE_OneParam(FCSInterfaceEvent, UClass*);
DECLARE_MULTICAST_DELEGATE_OneParam(FCSEnumEvent, UEnum*);

UCLASS()
class UNREALSHARPCORE_API UCSManager : public UObject
{
	GENERATED_BODY()
public:
	
	static UCSManager& GetOrCreate();
	static UCSManager& Get();

	// The outermost package for all managed packages. If namespace support is off, this is the only package that will be used.
	UPackage* GetGlobalUnrealSharpPackage() const { return GlobalUnrealSharpPackage; }
	UPackage* FindManagedPackage(FCSNamespace Namespace);
	
	TSharedPtr<FCSAssembly> LoadAssemblyByPath(const FString& AssemblyPath, bool bIsCollectible = true);

	// Load an assembly by name that exists in the ProjectRoot/Binaries/Managed folder
	TSharedPtr<FCSAssembly> LoadUserAssemblyByName(const FName AssemblyName, bool bIsCollectible = true);

	// Load an assembly by name that exists in the UnrealSharp/Binaries/Managed folder
	TSharedPtr<FCSAssembly> LoadPluginAssemblyByName(const FName AssemblyName, bool bIsCollectible = true);
	
	TSharedPtr<FCSAssembly> FindOwningAssembly(UClass* Class) const;
	TSharedPtr<FCSAssembly> FindOwningAssembly(const UObject* Object) const;
	
	TSharedPtr<FCSAssembly> FindAssembly(FName AssemblyName) const;
	TSharedPtr<FCSAssembly> FindOrLoadAssembly(FName AssemblyName);
	
	FGCHandle FindManagedObject(UObject* Object) const;

	void SetCurrentWorldContext(UObject* WorldContext) { CurrentWorldContext = WorldContext; }
	UObject* GetCurrentWorldContext() const { return CurrentWorldContext.Get(); }

	const FCSManagedPluginCallbacks& GetManagedPluginsCallbacks() const { return ManagedPluginsCallbacks; }

	FOnManagedAssemblyLoaded& OnManagedAssemblyLoadedEvent() { return OnManagedAssemblyLoaded; }
	FOnAssembliesReloaded& OnAssembliesLoadedEvent() { return OnAssembliesLoaded; }

#if WITH_EDITOR
	FCSClassEvent& OnNewClassEvent() { return OnNewClass; }
	FCSStructEvent& OnNewStructEvent() { return OnNewStruct; }
	FCSInterfaceEvent& OnNewInterfaceEvent() { return OnNewInterface; }
	FCSEnumEvent& OnNewEnumEvent() { return OnNewEnum; }

	FCSInterfaceEvent& OnInterfaceReloadedEvent() { return OnInterfaceReloaded; }
	FCSClassEvent& OnClassReloadedEvent() { return OnClassReloaded; }
	FCSStructEvent& OnStructReloadedEvent() { return OnStructReloaded; }
	FCSEnumEvent& OnEnumReloadedEvent() { return OnEnumReloaded; }
		
	FSimpleMulticastDelegate& OnProcessedPendingClassesEvent() { return OnProcessedPendingClasses; }
#endif
	
	void ForEachManagedPackage(const TFunction<void(UPackage*)>& Callback) const;
	void ForEachManagedField(const TFunction<void(UObject*)>& Callback) const;
	bool IsManagedPackage(const UPackage* Package) const;
	bool IsManagedField(const UObject* Field) const;

	bool IsLoadingAnyAssembly() const;

private:

	friend FUnrealSharpCoreModule;

	void Initialize();
	static void Shutdown();

	load_assembly_and_get_function_pointer_fn InitializeHostfxr() const;
	load_assembly_and_get_function_pointer_fn InitializeHostfxrSelfContained() const;
	
	bool LoadRuntimeHost();
	bool InitializeDotNetRuntime();

	bool LoadAllUserAssemblies();

	static UCSManager* Instance;

	UPROPERTY()
	TObjectPtr<UPackage> GlobalUnrealSharpPackage;

	UPROPERTY()
	TSet<TObjectPtr<UPackage>> AllPackages;
 
	UPROPERTY()
	TWeakObjectPtr<UObject> CurrentWorldContext;
	
	TMap<FName, TSharedPtr<FCSAssembly>> LoadedAssemblies;

	FOnManagedAssemblyLoaded OnManagedAssemblyLoaded;
	FOnAssembliesReloaded OnAssembliesLoaded;

#if WITH_EDITORONLY_DATA
	FCSClassEvent OnNewClass;
	FCSStructEvent OnNewStruct;
	FCSInterfaceEvent OnNewInterface;
	FCSEnumEvent OnNewEnum;

	FCSClassEvent OnClassReloaded;
	FCSStructEvent OnStructReloaded;
	FCSInterfaceEvent OnInterfaceReloaded;
	FCSEnumEvent OnEnumReloaded;

	FSimpleMulticastDelegate OnProcessedPendingClasses;
#endif
	
	FCSManagedPluginCallbacks ManagedPluginsCallbacks;
	
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