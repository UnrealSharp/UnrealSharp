#pragma once

#include <coreclr_delegates.h>
#include <hostfxr.h>
#include "CSAssembly.h"
#include "CSManagedCallbacksCache.h"

struct FCSTypeReferenceMetaData;
class FUSTypeFactory;
class UObject;
class FUSManagedObject;

struct FGCHandle;
struct FCSAssembly;

DECLARE_MULTICAST_DELEGATE(FOnUnrealSharpInitialized);

using FInitializeRuntimeHost = bool (*)(const TCHAR*, FCSManagedPluginCallbacks*, FCSManagedCallbacks::FManagedCallbacks*, const void*);

class CSHARPFORUE_API FCSManager : public FUObjectArray::FUObjectDeleteListener
{
public:

	static FCSManager& Get();

	void InitializeUnrealSharp();

	static UPackage* GetUnrealSharpPackage();

	TSharedPtr<FCSAssembly> LoadAssembly(const FString& AssemblyPath);
	bool UnloadAssembly(const FString& AssemblyName);

	FGCHandle CreateNewManagedObject(UObject* Object, UClass* Class);
	FGCHandle CreateNewManagedObject(UObject* Object, uint8* TypeHandle);
	
	FGCHandle FindManagedObject(UObject* Object);
	
	void RemoveManagedObject(UObject* Object);

	FOnUnrealSharpInitialized& OnUnrealSharpInitializedEvent() { return OnUnrealSharpInitialized; }
	bool IsInitialized() const { return bIsInitialized; }

	uint8* GetTypeHandle(const FString& AssemblyName, const FString& Namespace, const FString& TypeName) const;
	uint8* GetTypeHandle(const FCSTypeReferenceMetaData& TypeMetaData) const;

	bool LoadUserAssembly();

	TMap<FName, TSharedPtr<FCSAssembly>> LoadedPlugins;
	TMap<UObject*, FGCHandle> UnmanagedToManagedMap;
	
	static inline FCSManagedPluginCallbacks ManagedPluginsCallbacks;

private:

	FOnUnrealSharpInitialized OnUnrealSharpInitialized;
	bool bIsInitialized = false;
	
	static UPackage* UnrealSharpPackage;
	
	bool LoadRuntimeHost();
	bool InitializeBindings();
	
	load_assembly_and_get_function_pointer_fn InitializeHostfxr() const;
	load_assembly_and_get_function_pointer_fn InitializeHostfxrSelfContained() const;

	// Begin FUObjectArray::FUObjectDeleteListener Api
	virtual void NotifyUObjectDeleted(const UObjectBase *Object, int32 Index) override;
	virtual void OnUObjectArrayShutdown() override;
	// End FUObjectArray::FUObjectDeleteListener Api
	
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
