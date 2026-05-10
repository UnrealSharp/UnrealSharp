#pragma once

#include <coreclr_delegates.h>
#include <hostfxr.h>
#include "CSManagedAssembly.h"
#include "CSManagedCallbacksCache.h"
#include "CSManager.generated.h"

class UCSTypeBuilderManager;
class UCSInterface;
class UCSEnum;
class UCSScriptStruct;
class FUnrealSharpCoreModule;
class UFunctionsExporter;
struct FCSNamespace;
struct FCSTypeReferenceReflectionData;

struct FCSManagedPluginCallbacks
{
	using LoadPluginCallback = FGCHandleIntPtr(__stdcall*)(const TCHAR*, bool);
	using UnloadPluginCallback = bool(__stdcall*)(const TCHAR*);

	LoadPluginCallback LoadPlugin = nullptr;
	UnloadPluginCallback UnloadPlugin = nullptr;
};

using FInitializeRuntimeHost = bool (*)(const TCHAR*, const TCHAR*, FCSManagedPluginCallbacks*, const void*, FCSManagedCallbacks::FManagedCallbacks*);

DECLARE_MULTICAST_DELEGATE_OneParam(FOnManagedAssemblyLoaded, const UCSManagedAssembly*);
DECLARE_MULTICAST_DELEGATE_OneParam(FOnManagedAssemblyUnloaded, const UCSManagedAssembly*);
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
	DECLARE_MULTICAST_DELEGATE_OneParam(FCSManagerInitializedEvent, UCSManager&);

	static UCSManager& Get()
	{
		if (!Instance)
		{
			check(IsInGameThread());
			Instance = NewObject<UCSManager>(GetTransientPackage(), "CSManager", RF_Public | RF_MarkAsRootSet);
		}
		return *Instance;
	}
	
	bool IsInitialized() const { return bIsInitialized; }

	UPackage* GetGlobalManagedPackage() const { return GlobalManagedPackage; }
	UPackage* FindOrAddManagedPackage(const FCSNamespace& Namespace);
	UPackage* GetPackage(FCSNamespace Namespace);

	UCSManagedAssembly* LoadAssemblyByPath(const FString& AssemblyPath, bool bIsCollectible = false);
	UCSManagedAssembly* LoadUserAssemblyByName(FName AssemblyName, bool bIsCollectible = false);
	UCSManagedAssembly* LoadPluginAssemblyByName(FName AssemblyName, bool bIsCollectible = false);

	UCSManagedAssembly* FindOwningAssembly(UClass* Class);
	UCSManagedAssembly* FindOwningAssembly(UScriptStruct* Struct);
	UCSManagedAssembly* FindOwningAssembly(UEnum* Enum);
	
	UCSManagedAssembly* FindAssembly(FName AssemblyName) const
	{
		return LoadedAssemblies.FindRef(AssemblyName);
	}

	UFUNCTION(meta = (ScriptMethod))
	UCSManagedAssembly* FindOrLoadAssembly(FName AssemblyName, bool bIsCollectible = false)
	{
		if (UCSManagedAssembly* Assembly = FindAssembly(AssemblyName))
		{
			return Assembly;
		}
		return LoadUserAssemblyByName(AssemblyName, bIsCollectible);
	}

	UFUNCTION(meta = (ScriptMethod))
	void GetLoadedAssemblies(TArray<UCSManagedAssembly*>& Assemblies) const
	{
		TArray<TObjectPtr<UCSManagedAssembly>> FoundAssemblies;
		LoadedAssemblies.GenerateValueArray(FoundAssemblies);
		Assemblies.Append(FoundAssemblies);
	}
	
	FGCHandle FindManagedObject(const UObject* Object);
	FGCHandle FindOrCreateManagedInterfaceWrapper(UObject* Object, UClass* InterfaceClass);

	void SetCurrentWorldContext(UObject* WorldContext) { CurrentWorldContext = WorldContext; }
	UObject* GetCurrentWorldContext() const { return CurrentWorldContext.Get(); }

	const FCSManagedPluginCallbacks& GetManagedPluginsCallbacks() const { return ManagedPluginsCallbacks; }
	
	void AddOrExecuteOnManagerInitialized(const FCSManagerInitializedEvent::FDelegate& Delegate);

	FOnManagedAssemblyLoaded& OnManagedAssemblyLoadedEvent() { return OnManagedAssemblyLoaded; }
	FOnManagedAssemblyUnloaded& OnManagedAssemblyUnloadedEvent() { return OnManagedAssemblyUnloaded; }
	FOnAssembliesReloaded& OnAssembliesLoadedEvent() { return OnAssembliesLoaded; }

#if WITH_EDITOR
	FCSClassEvent& OnNewClassEvent() { return OnNewClass; }
	FCSStructEvent& OnNewStructEvent() { return OnNewStruct; }
	FCSInterfaceEvent& OnNewInterfaceEvent() { return OnNewInterface; }
	FCSEnumEvent& OnNewEnumEvent() { return OnNewEnum; }
#endif

	void ForEachManagedPackage(const TFunction<void(UPackage*)>& Callback) const
	{
		for (UPackage* Package : AllPackages)
		{
			Callback(Package);
		}
	}
	void ForEachManagedField(const TFunction<void(UObject*)>& Callback) const;

	bool IsManagedPackage(const UPackage* Package) const { return AllPackages.Contains(Package); }
	bool IsManagedType(const UObject* Field) const { return IsManagedPackage(Field->GetOutermost()); }
	bool IsLoadingAnyAssembly() const;

	void ActivateSubsystemClass(TSubclassOf<USubsystem> SubsystemClass);

private:
	friend UCSManagedAssembly;
	friend FUnrealSharpCoreModule;

	void Initialize();
	static void Shutdown() { Instance = nullptr; }

	load_assembly_and_get_function_pointer_fn InitializeNativeHost() const;

	bool LoadRuntimeHost();
	bool InitializeDotNetRuntime();
	bool LoadAllUserAssemblies();

	// FUObjectDeleteListener overrides
	virtual void NotifyUObjectDeleted(const UObjectBase* Object, int32 Index) override;
	virtual void OnUObjectArrayShutdown() override { GUObjectArray.RemoveUObjectDeleteListener(this); }
	// End FUObjectDeleteListener overrides
	void OnEnginePreExit() { GUObjectArray.RemoveUObjectDeleteListener(this); }

	void OnModulesChanged(FName InModuleName, EModuleChangeReason InModuleChangeReason);
	void InitializeSubsystems();

	template<typename T, typename TUserDefined>
	UCSManagedAssembly* FindOwningAssemblyGeneric(T* Object)
	{
		TRACE_CPUPROFILER_EVENT_SCOPE(UCSManager::FindOwningAssemblyGeneric);

		if (const ICSManagedTypeInterface* ManagedType = Cast<ICSManagedTypeInterface>(Object))
		{
			return ManagedType->GetOwningAssembly();
		}

		if (Cast<TUserDefined>(Object))
		{
			return nullptr;
		}

		const uint32 ClassID = Object->GetUniqueID();
		if (TObjectPtr<UCSManagedAssembly> Assembly = NativeClassToAssemblyMap.FindRef(ClassID))
		{
			return Assembly;
		}

		return FindOwningAssemblySlow(Object);
	}

	UCSManagedAssembly* FindOwningAssemblySlow(UField* Field);

	UPROPERTY()
	TArray<TObjectPtr<UPackage>> AllPackages;

	UPROPERTY()
	TObjectPtr<UPackage> GlobalManagedPackage;

	UPROPERTY(Transient)
	TArray<TSubclassOf<USubsystem>> PendingSubsystems;

	TMap<uint32, TSharedPtr<FGCHandle>> ManagedObjectHandles;
	TMap<uint32, TMap<uint32, TSharedPtr<FGCHandle>>> ManagedInterfaceWrappers;
	
	UPROPERTY()
	TMap<uint32, TObjectPtr<UCSManagedAssembly>> NativeClassToAssemblyMap;

	UPROPERTY()
	TMap<FName, TObjectPtr<UCSManagedAssembly>> LoadedAssemblies;

	TWeakObjectPtr<UObject> CurrentWorldContext;
	
	FCSManagerInitializedEvent OnCSManagerInitialized;
	bool bIsInitialized = false;

	FOnManagedAssemblyLoaded OnManagedAssemblyLoaded;
	FOnManagedAssemblyUnloaded OnManagedAssemblyUnloaded;
	FOnAssembliesReloaded OnAssembliesLoaded;

#if WITH_EDITORONLY_DATA
	FCSClassEvent OnNewClass;
	FCSStructEvent OnNewStruct;
	FCSInterfaceEvent OnNewInterface;
	FCSEnumEvent OnNewEnum;
#endif

	FCSManagedPluginCallbacks ManagedPluginsCallbacks;

	hostfxr_initialize_for_dotnet_command_line_fn Hostfxr_Initialize_For_Dotnet_Command_Line = nullptr;
	hostfxr_initialize_for_runtime_config_fn Hostfxr_Initialize_For_Runtime_Config = nullptr;
	hostfxr_get_runtime_delegate_fn Hostfxr_Get_Runtime_Delegate = nullptr;
	hostfxr_close_fn Hostfxr_Close = nullptr;

	void* RuntimeHost = nullptr;
	
	static UCSManager* Instance;
};