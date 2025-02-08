#pragma once

#include <coreclr_delegates.h>
#include <hostfxr.h>
#include "CSAssembly.h"
#include "CSManagedCallbacksCache.h"
#include "CSManager.generated.h"

struct FCSTypeReferenceMetaData;

using FInitializeRuntimeHost = bool (*)(const TCHAR*, const TCHAR*, FCSManagedPluginCallbacks*, FCSManagedCallbacks::FManagedCallbacks*, const void*);

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
	static void Shutdown();
	
	UPackage* GetUnrealSharpPackage() const { return UnrealSharpPackage; }

	bool LoadAllUserAssemblies();
	
	TSharedPtr<FCSAssembly> LoadAssembly(const FString& AssemblyPath);
	TSharedPtr<FCSAssembly> LoadAssemblyByName(const FName AssemblyName);
	
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

	FCSClassEvent& OnNewClassEvent() { return OnNewClass; }
	FCSStructEvent& OnNewStructEvent() { return OnNewStruct; }
	FCSInterfaceEvent& OnNewInterfaceEvent() { return OnNewInterface; }
	FCSEnumEvent& OnNewEnumEvent() { return OnNewEnum; }

	FCSInterfaceEvent& OnInterfaceReloadedEvent() { return OnInterfaceReloaded; }
	FCSClassEvent& OnClassReloadedEvent() { return OnClassReloaded; }
	FCSStructEvent& OnStructReloadedEvent() { return OnStructReloaded; }
	FCSEnumEvent& OnEnumReloadedEvent() { return OnEnumReloaded; }
	
	FSimpleMulticastDelegate& OnProcessedPendingClassesEvent() { return OnProcessedPendingClasses; }

private:

	void Initialize();

	load_assembly_and_get_function_pointer_fn InitializeHostfxr() const;
	load_assembly_and_get_function_pointer_fn InitializeHostfxrSelfContained() const;
	
	bool LoadRuntimeHost();
	bool InitializeRuntime();

	static UCSManager* Instance;

	UPROPERTY()
	TObjectPtr<UPackage> UnrealSharpPackage;

	UPROPERTY()
	TWeakObjectPtr<UObject> CurrentWorldContext;
	
	TMap<FName, TSharedPtr<FCSAssembly>> LoadedPlugins;

	FOnManagedAssemblyLoaded OnManagedAssemblyLoaded;
	FOnAssembliesReloaded OnAssembliesLoaded;

	FCSClassEvent OnNewClass;
	FCSStructEvent OnNewStruct;
	FCSInterfaceEvent OnNewInterface;
	FCSEnumEvent OnNewEnum;

	FCSClassEvent OnClassReloaded;
	FCSStructEvent OnStructReloaded;
	FCSInterfaceEvent OnInterfaceReloaded;
	FCSEnumEvent OnEnumReloaded;
	
	FSimpleMulticastDelegate OnProcessedPendingClasses;
	

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