#pragma once

#include "CSBindsRegistry.h"
#include "CSManagedAssembly.h"
#include "CSObjectID.h"
#include "CSManager.generated.h"

class UCSScriptStruct;
class UCSEnum;

DECLARE_MULTICAST_DELEGATE_OneParam(FCSClassEvent, UCSClass*);
DECLARE_MULTICAST_DELEGATE_OneParam(FCSStructEvent, UCSScriptStruct*);
DECLARE_MULTICAST_DELEGATE_OneParam(FCSInterfaceEvent, UCSInterface*);
DECLARE_MULTICAST_DELEGATE_OneParam(FCSEnumEvent, UCSEnum*);

DECLARE_MULTICAST_DELEGATE_OneParam(FCSManagerInitializedEvent, class UCSManager&);

UCLASS(Transient)
class UCSManager : public UObject, public FUObjectArray::FUObjectDeleteListener
{
	GENERATED_BODY()
public:
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
	
	UNREALSHARPCORE_API UPackage* FindOrAddManagedPackage(const FCSNamespace& Namespace);
	UNREALSHARPCORE_API UPackage* GetPackage(const FCSNamespace& Namespace);
	UNREALSHARPCORE_API UPackage* GetGlobalManagedPackage() const { return GlobalManagedPackage; }
	UNREALSHARPCORE_API const TArray<TObjectPtr<UPackage>>& GetManagedPackages() const { return ManagedPackages; }

	UNREALSHARPCORE_API UCSManagedAssembly* LoadAssemblyByPath(const FString& AssemblyPath, bool bIsCollectible = false);
	UNREALSHARPCORE_API UCSManagedAssembly* LoadUserAssemblyByName(FName AssemblyName, bool bIsCollectible = false);
	UNREALSHARPCORE_API UCSManagedAssembly* LoadPluginAssemblyByName(FName AssemblyName, bool bIsCollectible = false);

	UNREALSHARPCORE_API UCSManagedAssembly* FindOwningAssembly(UClass* Class);
	UNREALSHARPCORE_API UCSManagedAssembly* FindOwningAssembly(UScriptStruct* Struct);
	UNREALSHARPCORE_API UCSManagedAssembly* FindOwningAssembly(UEnum* Enum);
	
	UNREALSHARPCORE_API UCSManagedAssembly* FindAssembly(FName AssemblyName) const { return Assemblies.FindRef(AssemblyName); }
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
		TArray<TObjectPtr<UCSManagedAssembly>> LoadedAssemblies;
		Assemblies.GenerateValueArray(LoadedAssemblies);
		
		OutAssemblies.Append(LoadedAssemblies);
	}
	
	UNREALSHARPCORE_API FGCHandle FindManagedObject(const UObject* Object);
	UNREALSHARPCORE_API FGCHandle FindManagedInterfaceWrapper(UObject* Object, UClass* InterfaceClass);
	
	UNREALSHARPCORE_API void AddOrExecuteOnManagerInitialized(const FCSManagerInitializedEvent::FDelegate& Delegate);

#if WITH_EDITOR
	UNREALSHARPCORE_API FCSClassEvent& OnNewClassEvent() { return OnNewClass; }
	UNREALSHARPCORE_API FCSStructEvent& OnNewStructEvent() { return OnNewStruct; }
	UNREALSHARPCORE_API FCSInterfaceEvent& OnNewInterfaceEvent() { return OnNewInterface; }
	UNREALSHARPCORE_API FCSEnumEvent& OnNewEnumEvent() { return OnNewEnum; }
#endif
	
	UNREALSHARPCORE_API bool IsManagedPackage(const UPackage* Package) const { return ManagedPackages.Contains(Package); }
	UNREALSHARPCORE_API bool IsManagedType(const UField* Field) const { return IsManagedPackage(Field->GetOutermost()); }
	UNREALSHARPCORE_API bool IsLoadingAnyAssembly() const;
	
	void SetCurrentWorldContext(UObject* WorldContext) { CurrentWorldContext = WorldContext; }
	UObject* GetCurrentWorldContext() const { return CurrentWorldContext.Get(); }
	
	TMap<FCSObjectID, TSharedPtr<FGCHandle>>& GetManagedObjectHandles() { return ManagedObjectHandles; }
	TMap<FCSObjectID, TMap<FCSObjectID, TSharedPtr<FGCHandle>>>& GetManagedInterfaceWrappers() { return ManagedInterfaceWrapperHandles; }

private:
	void InitialAssemblyLoad();
	void OnEnginePreExit() { GUObjectArray.RemoveUObjectDeleteListener(this); }

	UPROPERTY(Transient)
	TArray<TObjectPtr<UPackage>> ManagedPackages;

	UPROPERTY(Transient)
	TObjectPtr<UPackage> GlobalManagedPackage;
	
	UPROPERTY(Transient)
	TMap<FCSObjectID, TObjectPtr<UCSManagedAssembly>> NativeTypeToAssembly;

	UPROPERTY(Transient)
	TMap<FName, TObjectPtr<UCSManagedAssembly>> Assemblies;
	
	TMap<FCSObjectID, TSharedPtr<FGCHandle>> ManagedObjectHandles;
	TMap<FCSObjectID, TMap<FCSObjectID, TSharedPtr<FGCHandle>>> ManagedInterfaceWrapperHandles;

	TWeakObjectPtr<UObject> CurrentWorldContext;
	
	FCSManagerInitializedEvent OnInitialized;

#if WITH_EDITORONLY_DATA
	FCSClassEvent OnNewClass;
	FCSStructEvent OnNewStruct;
	FCSInterfaceEvent OnNewInterface;
	FCSEnumEvent OnNewEnum;
#endif
	
	bool bHasInitialized = false;
	
	static UCSManager* Instance;
};