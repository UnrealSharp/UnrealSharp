#pragma once

#include <coreclr_delegates.h>
#include <hostfxr.h>
#include "CSAssembly.h"
#include "CSManagedCallbacksCache.h"

struct FCSTypeReferenceMetaData;
DECLARE_MULTICAST_DELEGATE(FOnUnrealSharpInitialized);

using FInitializeRuntimeHost = bool (*)(const TCHAR*, FCSManagedPluginCallbacks*, FCSManagedCallbacks::FManagedCallbacks*, const void*);

class CSHARPFORUE_API FCSManager final : public FUObjectArray::FUObjectDeleteListener
{
public:

	void InitializeUnrealSharp();

	static FCSManager& Get();
	static UPackage* GetUnrealSharpPackage() { return Get().UnrealSharpPackage; }

	TSharedPtr<FCSAssembly> LoadAssembly(const FString& AssemblyPath);
	bool UnloadAssembly(const FString& AssemblyName);

	FGCHandle CreateNewManagedObject(UObject* Object, UClass* Class);
	FGCHandle CreateNewManagedObject(UObject* Object, uint8* TypeHandle);
	
	FGCHandle FindManagedObject(UObject* Object);

	FOnUnrealSharpInitialized& OnUnrealSharpInitializedEvent() { return OnUnrealSharpInitialized; }
	bool IsInitialized() const { return bIsInitialized; }

	uint8* GetTypeHandle(uint8* AssemblyHandle, const FString& Namespace, const FString& TypeName) const;
	uint8* GetTypeHandle(const FString& AssemblyName, const FString& Namespace, const FString& TypeName) const;
	uint8* GetTypeHandle(const FCSTypeReferenceMetaData& TypeMetaData) const;

	bool LoadUserAssembly();

	TMap<FName, TSharedPtr<FCSAssembly>> LoadedPlugins;
	TMap<const UObjectBase*, FGCHandle> UnmanagedToManagedMap;
	
	static FCSManagedPluginCallbacks ManagedPluginsCallbacks;

private:

	load_assembly_and_get_function_pointer_fn InitializeHostfxr() const;
	load_assembly_and_get_function_pointer_fn InitializeHostfxrSelfContained() const;
	
	bool LoadRuntimeHost();
	bool InitializeBindings();
	
	void RemoveManagedObject(const UObjectBase* Object);

	// Begin FUObjectArray::FUObjectDeleteListener Api
	virtual void NotifyUObjectDeleted(const UObjectBase *Object, int32 Index) override
	{
		RemoveManagedObject(Object);
	}
	
	virtual void OnUObjectArrayShutdown() override
	{
		GUObjectArray.RemoveUObjectDeleteListener(this);
	}
	// End FUObjectArray::FUObjectDeleteListener Api

	UPackage* UnrealSharpPackage;
	
	FOnUnrealSharpInitialized OnUnrealSharpInitialized;
	bool bIsInitialized = false;
	
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
