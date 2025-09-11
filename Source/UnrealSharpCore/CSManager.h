#pragma once

#include <coreclr_delegates.h>
#include <hostfxr.h>
#include "CSAssembly.h"
#include "CSManagedCallbacksCache.h"
#include "CSManager.generated.h"

class UCSTypeBuilderManager;
class UCSInterface;
class UCSEnum;
class UCSScriptStruct;
class FUnrealSharpCoreModule;
class UFunctionsExporter;
struct FCSNamespace;
struct FCSTypeReferenceMetaData;

struct FCSManagedPluginCallbacks
{
	using LoadPluginCallback = FGCHandleIntPtr(__stdcall*)(const TCHAR*, bool);
	using UnloadPluginCallback = bool(__stdcall*)(const TCHAR*);

	LoadPluginCallback LoadPlugin = nullptr;
	UnloadPluginCallback UnloadPlugin = nullptr;
};

using FInitializeRuntimeHost = bool (*)(const TCHAR*, const TCHAR*, FCSManagedPluginCallbacks*, const void*, FCSManagedCallbacks::FManagedCallbacks*);

DECLARE_MULTICAST_DELEGATE_OneParam(FOnManagedAssemblyLoaded, const FName&);
DECLARE_MULTICAST_DELEGATE_OneParam(FOnManagedAssemblyUnloaded, const FName&);
DECLARE_MULTICAST_DELEGATE(FOnAssembliesReloaded);

DECLARE_MULTICAST_DELEGATE_OneParam(FCSClassEvent, UCSClass*);
DECLARE_MULTICAST_DELEGATE_OneParam(FCSStructEvent, UCSScriptStruct*);
DECLARE_MULTICAST_DELEGATE_OneParam(FCSInterfaceEvent, UCSInterface*);
DECLARE_MULTICAST_DELEGATE_OneParam(FCSEnumEvent, UCSEnum*);

UCLASS()
class UNREALSHARPCORE_API UCSManager : public UObject, public FUObjectArray::FUObjectDeleteListener
{
    GENERATED_BODY()
public:

    static UCSManager& GetOrCreate()
    {
        if (!Instance)
        {
            Instance = NewObject<UCSManager>(GetTransientPackage(), TEXT("CSManager"), RF_Public | RF_MarkAsRootSet);
        }

        return *Instance;
    }

    static UCSManager& Get() { return *Instance; }

    // The outermost package for all managed packages. If namespace support is off, this is the only package that will be used.
    UPackage* GetGlobalManagedPackage() const { return GlobalManagedPackage; }
    UPackage* FindOrAddManagedPackage(FCSNamespace Namespace);

    UCSAssembly* LoadAssemblyByPath(const FString& AssemblyPath, bool bIsCollectible = true);

    // Load an assembly by name that exists in the ProjectRoot/Binaries/Managed folder
    UCSAssembly* LoadUserAssemblyByName(const FName AssemblyName, bool bIsCollectible = true);

    // Load an assembly by name that exists in the UnrealSharp/Binaries/Managed folder
    UCSAssembly* LoadPluginAssemblyByName(const FName AssemblyName, bool bIsCollectible = true);

    UCSAssembly* FindOwningAssembly(UClass* Class);

    UCSAssembly* FindOwningAssembly(UScriptStruct* Struct);

    UCSAssembly* FindAssembly(FName AssemblyName) const
    {
        return LoadedAssemblies.FindRef(AssemblyName);
    }

    UCSAssembly* FindOrLoadAssembly(FName AssemblyName)
    {
        if (UCSAssembly* Assembly = FindAssembly(AssemblyName))
        {
            return Assembly;
        }

        return LoadUserAssemblyByName(AssemblyName);
    }
	
    FGCHandle FindManagedObject(const UObject* Object);
    FGCHandle FindOrCreateManagedInterfaceWrapper(UObject* Object, UClass* InterfaceClass);

    void SetCurrentWorldContext(UObject* WorldContext) { CurrentWorldContext = WorldContext; }
    UObject* GetCurrentWorldContext() const { return CurrentWorldContext.Get(); }

    const FCSManagedPluginCallbacks& GetManagedPluginsCallbacks() const { return ManagedPluginsCallbacks; }

    FOnManagedAssemblyLoaded& OnManagedAssemblyLoadedEvent() { return OnManagedAssemblyLoaded; }
    FOnManagedAssemblyUnloaded& OnManagedAssemblyUnloadedEvent() { return OnManagedAssemblyUnloaded; }
	FOnAssembliesReloaded& OnAssembliesLoadedEvent() { return OnAssembliesLoaded; }

#if WITH_EDITOR
	FCSClassEvent& OnNewClassEvent() { return OnNewClass; }
	FCSStructEvent& OnNewStructEvent() { return OnNewStruct; }
	FCSInterfaceEvent& OnNewInterfaceEvent() { return OnNewInterface; }
	FCSEnumEvent& OnNewEnumEvent() { return OnNewEnum; }

	FSimpleMulticastDelegate& OnProcessedPendingClassesEvent() { return OnProcessedPendingClasses; }
#endif

	void ForEachManagedPackage(const TFunction<void(UPackage*)>& Callback) const
	{
		for (UPackage* Package : AllPackages)
		{
			Callback(Package);
		}
	}
	void ForEachManagedField(const TFunction<void(UObject*)>& Callback) const;

	bool IsManagedPackage(const UPackage* Package) const { 	return AllPackages.Contains(Package); }
	UPackage* GetPackage(const FCSNamespace Namespace);

	bool IsManagedType(const UObject* Field) const { return IsManagedPackage(Field->GetOutermost()); }

	bool IsLoadingAnyAssembly() const;

	void AddDynamicSubsystemClass(TSubclassOf<UDynamicSubsystem> SubsystemClass);

	UCSTypeBuilderManager* GetTypeBuilderManager() const { return TypeBuilderManager; }

private:

	friend UCSAssembly;
	friend FUnrealSharpCoreModule;

	void Initialize();
	static void Shutdown() { Instance = nullptr; }

	load_assembly_and_get_function_pointer_fn InitializeNativeHost() const;

	bool LoadRuntimeHost();
	bool InitializeDotNetRuntime();
	bool LoadAllUserAssemblies();

	// UObjectArray listener interface
	virtual void NotifyUObjectDeleted(const UObjectBase* Object, int32 Index) override;
	virtual void OnUObjectArrayShutdown() override { GUObjectArray.RemoveUObjectDeleteListener(this); }
	void OnEnginePreExit() { GUObjectArray.RemoveUObjectDeleteListener(this); }
	// End of interface

	void OnModulesChanged(FName InModuleName, EModuleChangeReason InModuleChangeReason);
	void TryInitializeDynamicSubsystems();

    UCSAssembly* FindOwningAssemblySlow(UField* Field);

	static UCSManager* Instance;

	UPROPERTY()
	TArray<TObjectPtr<UPackage>> AllPackages;

	UPROPERTY()
	TObjectPtr<UPackage> GlobalManagedPackage;

	UPROPERTY(Transient)
	TArray<TSubclassOf<UDynamicSubsystem>> PendingDynamicSubsystemClasses;

	UPROPERTY(Transient)
	TObjectPtr<UCSTypeBuilderManager> TypeBuilderManager;

	// Handles to all active UObjects that has a C# counterpart. The key is the unique ID of the UObject.
	TMap<uint32, TSharedPtr<FGCHandle>> ManagedObjectHandles;

	// Handles all active UObjects that have interface wrappers in C#. The primary key is the unique ID of the UObject.
	// The second key is the unique ID of the interface class.
	TMap<uint32, TMap<uint32, TSharedPtr<FGCHandle>>> ManagedInterfaceWrappers;
	
	// Map to cache assemblies that native classes are associated with, for quick lookup.
	UPROPERTY()
	TMap<uint32, TObjectPtr<UCSAssembly>> NativeClassToAssemblyMap;

	UPROPERTY()
	TMap<FName, TObjectPtr<UCSAssembly>> LoadedAssemblies;

	TWeakObjectPtr<UObject> CurrentWorldContext;

	FOnManagedAssemblyLoaded OnManagedAssemblyLoaded;
    FOnManagedAssemblyUnloaded OnManagedAssemblyUnloaded;
	FOnAssembliesReloaded OnAssembliesLoaded;

#if WITH_EDITORONLY_DATA
	FCSClassEvent OnNewClass;
	FCSStructEvent OnNewStruct;
	FCSInterfaceEvent OnNewInterface;
	FCSEnumEvent OnNewEnum;

	FSimpleMulticastDelegate OnProcessedPendingClasses;
#endif

	FCSManagedPluginCallbacks ManagedPluginsCallbacks;

	//.NET Core Host API
	hostfxr_initialize_for_dotnet_command_line_fn Hostfxr_Initialize_For_Dotnet_Command_Line = nullptr;
	hostfxr_initialize_for_runtime_config_fn Hostfxr_Initialize_For_Runtime_Config = nullptr;
	hostfxr_get_runtime_delegate_fn Hostfxr_Get_Runtime_Delegate = nullptr;
	hostfxr_close_fn Hostfxr_Close = nullptr;

	void* RuntimeHost = nullptr;
	//End
};
