#pragma once

#include <coreclr_delegates.h>
#include <hostfxr.h>
#include "CSAssembly.h"
#include "CSManagedCallbacksCache.h"
#include "CSManager.generated.h"

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
	
	TSharedPtr<FCSAssembly> LoadAssemblyByPath(const FString& AssemblyPath, bool bIsCollectible = true);

	// Load an assembly by name that exists in the ProjectRoot/Binaries/Managed folder
	TSharedPtr<FCSAssembly> LoadUserAssemblyByName(const FName AssemblyName, bool bIsCollectible = true);

	// Load an assembly by name that exists in the UnrealSharp/Binaries/Managed folder
	TSharedPtr<FCSAssembly> LoadPluginAssemblyByName(const FName AssemblyName, bool bIsCollectible = true);
	
	TSharedPtr<FCSAssembly> FindOwningAssembly(UClass* Class);
	
	TSharedPtr<FCSAssembly> FindAssembly(FName AssemblyName) const 
	{
		return LoadedAssemblies.FindRef(AssemblyName);
	}
	
	TSharedPtr<FCSAssembly> FindOrLoadAssembly(FName AssemblyName)
	{
		if (TSharedPtr<FCSAssembly> Assembly = FindAssembly(AssemblyName))
		{
			return Assembly;
		}
	
		return LoadUserAssemblyByName(AssemblyName);
	}
	
	FGCHandle FindManagedObject(UObject* Object);

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
	
	void ForEachManagedPackage(const TFunction<void(UPackage*)>& Callback) const
	{
		for (UPackage* Package : AllPackages)
		{
			Callback(Package);
		}
	}
	void ForEachManagedField(const TFunction<void(UObject*)>& Callback) const;
	
	bool IsManagedPackage(const UPackage* Package) const { 	return AllPackages.Contains(Package); }
	bool IsManagedField(const UObject* Field) const { return IsManagedPackage(Field->GetOutermost()); }

	bool IsLoadingAnyAssembly() const;

private:

	friend FCSAssembly;
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

	static UCSManager* Instance;

	UPROPERTY()
	TArray<TObjectPtr<UPackage>> AllPackages;

	UPROPERTY()
	TObjectPtr<UPackage> GlobalManagedPackage;

	// Handles to all active UObjects that has a C# counterpart. The key is the unique ID of the UObject.
	TMap<uint32, TSharedPtr<FGCHandle>> ManagedObjectHandles;
	
	// Map to cache assemblies that native classes are associated with, for quick lookup.
	TMap<uint32, TSharedPtr<FCSAssembly>> NativeClassToAssemblyMap;
	
	TMap<FName, TSharedPtr<FCSAssembly>> LoadedAssemblies;
	
	TWeakObjectPtr<UObject> CurrentWorldContext;

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