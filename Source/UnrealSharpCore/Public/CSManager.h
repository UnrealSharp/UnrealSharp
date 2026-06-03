#pragma once

#include <coreclr_delegates.h>
#include <hostfxr.h>
#include "CSManagedAssembly.h"
#include "CSManagedCallbacksCache.h"
#include "CSObjectID.h"
#include "DotNet/CSDotNetRuntimeHost.h"
#include "CSManager.generated.h"

struct FCSManagedPluginCallbacks;
class UCSTypeBuilderManager;
class UCSInterface;
class UCSEnum;
class UCSScriptStruct;
class FUnrealSharpCoreModule;
class UFunctionsExporter;
struct FCSNamespace;
struct FCSTypeReferenceReflectionData;

DECLARE_MULTICAST_DELEGATE_OneParam(FOnManagedAssemblyLoaded, const UCSManagedAssembly*);
DECLARE_MULTICAST_DELEGATE_OneParam(FOnManagedAssemblyUnloaded, const UCSManagedAssembly*);
DECLARE_MULTICAST_DELEGATE(FOnAssembliesReloaded);

DECLARE_MULTICAST_DELEGATE_OneParam(FCSClassEvent, UCSClass*);
DECLARE_MULTICAST_DELEGATE_OneParam(FCSStructEvent, UCSScriptStruct*);
DECLARE_MULTICAST_DELEGATE_OneParam(FCSInterfaceEvent, UCSInterface*);
DECLARE_MULTICAST_DELEGATE_OneParam(FCSEnumEvent, UCSEnum*);

UCLASS(Transient)
class UCSManager : public UObject, public FUObjectArray::FUObjectDeleteListener
{
	GENERATED_BODY()
public:
	DECLARE_MULTICAST_DELEGATE_OneParam(FCSManagerInitializedEvent, UCSManager&);
	
	void Initialize();
	
	// FUObjectDeleteListener overrides
	virtual void NotifyUObjectDeleted(const UObjectBase* Object, int32 Index) override;
	virtual void OnUObjectArrayShutdown() override { GUObjectArray.RemoveUObjectDeleteListener(this); }
	// End FUObjectDeleteListener overrides
	
	UNREALSHARPCORE_API static UCSManager& Get()
	{
		if (!Instance)
		{
			check(IsInGameThread());
			Instance = NewObject<UCSManager>(GetTransientPackage(), "CSManager", RF_Public | RF_MarkAsRootSet);
		}
		return *Instance;
	}

	UNREALSHARPCORE_API UPackage* GetGlobalManagedPackage() const { return GlobalManagedPackage; }
	UNREALSHARPCORE_API UPackage* FindOrAddManagedPackage(const FCSNamespace& Namespace);
	UNREALSHARPCORE_API UPackage* GetPackage(FCSNamespace Namespace);

	UNREALSHARPCORE_API UCSManagedAssembly* LoadAssemblyByPath(const FString& AssemblyPath, bool bIsCollectible = false);
	UNREALSHARPCORE_API UCSManagedAssembly* LoadUserAssemblyByName(FName AssemblyName, bool bIsCollectible = false);
	UNREALSHARPCORE_API UCSManagedAssembly* LoadPluginAssemblyByName(FName AssemblyName, bool bIsCollectible = false);

	UNREALSHARPCORE_API UCSManagedAssembly* FindOwningAssembly(UClass* Class);
	UNREALSHARPCORE_API UCSManagedAssembly* FindOwningAssembly(UScriptStruct* Struct);
	UNREALSHARPCORE_API UCSManagedAssembly* FindOwningAssembly(UEnum* Enum);
	
	UNREALSHARPCORE_API UCSManagedAssembly* FindAssembly(FName AssemblyName) const
	{
		return Assemblies.FindRef(AssemblyName);
	}
	
	UNREALSHARPCORE_API UCSManagedAssembly* FindOrLoadAssembly(FName AssemblyName, bool bIsCollectible = false)
	{
		if (UCSManagedAssembly* Assembly = FindAssembly(AssemblyName))
		{
			return Assembly;
		}
		
		return LoadUserAssemblyByName(AssemblyName, bIsCollectible);
	}
	
	UNREALSHARPCORE_API void GetLoadedAssemblies(TArray<UCSManagedAssembly*>& OutAssemblies) const
	{
		TArray<TObjectPtr<UCSManagedAssembly>> FoundAssemblies;
		Assemblies.GenerateValueArray(FoundAssemblies);
		OutAssemblies.Append(OutAssemblies);
	}
	
	UNREALSHARPCORE_API FGCHandle FindManagedObject(const UObject* Object);
	UNREALSHARPCORE_API FGCHandle FindOrCreateManagedInterfaceWrapper(UObject* Object, UClass* InterfaceClass);
	
	UNREALSHARPCORE_API void AddOrExecuteOnManagerInitialized(const FCSManagerInitializedEvent::FDelegate& Delegate);

	UNREALSHARPCORE_API FOnManagedAssemblyLoaded& OnManagedAssemblyLoadedEvent() { return OnManagedAssemblyLoaded; }
	UNREALSHARPCORE_API FOnManagedAssemblyUnloaded& OnManagedAssemblyUnloadedEvent() { return OnManagedAssemblyUnloaded; }

#if WITH_EDITOR
	UNREALSHARPCORE_API FCSClassEvent& OnNewClassEvent() { return OnNewClass; }
	UNREALSHARPCORE_API FCSStructEvent& OnNewStructEvent() { return OnNewStruct; }
	UNREALSHARPCORE_API FCSInterfaceEvent& OnNewInterfaceEvent() { return OnNewInterface; }
	UNREALSHARPCORE_API FCSEnumEvent& OnNewEnumEvent() { return OnNewEnum; }
#endif

	UNREALSHARPCORE_API void ForEachManagedPackage(const TFunction<void(UPackage*)>& Callback) const
	{
		for (UPackage* Package : AllPackages)
		{
			Callback(Package);
		}
	}
	UNREALSHARPCORE_API void ForEachManagedField(const TFunction<void(UObject*)>& Callback) const;

	UNREALSHARPCORE_API bool IsManagedPackage(const UPackage* Package) const { return AllPackages.Contains(Package); }
	UNREALSHARPCORE_API bool IsManagedType(const UObject* Field) const { return IsManagedPackage(Field->GetOutermost()); }
	UNREALSHARPCORE_API bool IsLoadingAnyAssembly() const;
	
	void SetCurrentWorldContext(UObject* WorldContext) { CurrentWorldContext = WorldContext; }
	UObject* GetCurrentWorldContext() const { return CurrentWorldContext.Get(); }
	
	TMap<FCSObjectID, TSharedPtr<FGCHandle>>& GetManagedObjectHandles() { return ManagedObjectHandles; }
	TMap<FCSObjectID, TMap<FCSObjectID, TSharedPtr<FGCHandle>>>& GetManagedInterfaceWrappers() { return ManagedInterfaceWrappers; }

private:
	void LoadAssemblies();
	
	void OnEnginePreExit() { GUObjectArray.RemoveUObjectDeleteListener(this); }

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

		const FCSObjectID ObjectID = Object->GetUniqueID();
		if (TObjectPtr<UCSManagedAssembly> Assembly = NativeClassToAssemblyMap.FindRef(ObjectID))
		{
			return Assembly;
		}

		return FindOwningAssemblySlow(Object);
	}

	UCSManagedAssembly* FindOwningAssemblySlow(UField* Field);

	UPROPERTY(Transient)
	TArray<TObjectPtr<UPackage>> AllPackages;

	UPROPERTY(Transient)
	TObjectPtr<UPackage> GlobalManagedPackage;
	
	UPROPERTY(Transient)
	TMap<FCSObjectID, TObjectPtr<UCSManagedAssembly>> NativeClassToAssemblyMap;

	UPROPERTY(Transient)
	TMap<FName, TObjectPtr<UCSManagedAssembly>> Assemblies;
	
	TMap<FCSObjectID, TSharedPtr<FGCHandle>> ManagedObjectHandles;
	TMap<FCSObjectID, TMap<FCSObjectID, TSharedPtr<FGCHandle>>> ManagedInterfaceWrappers;

	TWeakObjectPtr<UObject> CurrentWorldContext;
	
	FCSManagerInitializedEvent OnCSManagerInitialized;

	FOnManagedAssemblyLoaded OnManagedAssemblyLoaded;
	FOnManagedAssemblyUnloaded OnManagedAssemblyUnloaded;
	
	bool bHasInitialized = false;

#if WITH_EDITORONLY_DATA
	FCSClassEvent OnNewClass;
	FCSStructEvent OnNewStruct;
	FCSInterfaceEvent OnNewInterface;
	FCSEnumEvent OnNewEnum;
#endif
	
	static UCSManager* Instance;
};