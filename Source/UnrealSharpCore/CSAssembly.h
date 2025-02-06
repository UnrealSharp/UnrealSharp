#pragma once

#include "CSManagedGCHandle.h"

#if !defined(_WIN32)
#define __stdcall
#endif

class UCSClass;
struct FCSharpClassInfo;
struct FCSharpInterfaceInfo;
struct FCSharpEnumInfo;
struct FCSharpStructInfo;

struct FCSManagedPluginCallbacks
{
	using LoadPluginCallback = GCHandleIntPtr(__stdcall*)(const TCHAR*);
	using UnloadPluginCallback = bool(__stdcall*)(const TCHAR*);
	
	LoadPluginCallback LoadPlugin = nullptr;
	UnloadPluginCallback UnloadPlugin = nullptr;
};

struct FCSAssembly : public TSharedFromThis<FCSAssembly>
{
	FCSAssembly(const FString& InAssemblyPath);

	bool Load(bool bProcessMetaData = true);
	bool Unload();
#if WITH_EDITOR
	bool Reload();
#endif
	bool IsValid() const;

	const GCHandleIntPtr& GetAssemblyHandle() const { return Assembly.Handle; }
	const FName& GetAssemblyName() const { return AssemblyName; }
	const FString& GetAssemblyPath() const { return AssemblyPath; }

	TSharedPtr<FGCHandle> GetTypeHandle(const FString& Namespace, const FString& TypeName);
	TSharedPtr<FGCHandle> GetTypeHandle(const UClass* Class);
	
	TSharedPtr<FGCHandle> GetMethodHandle(const TSharedPtr<FGCHandle>& TypeHandle, const FString& MethodName);
	TSharedPtr<FGCHandle> GetMethodHandle(const UCSClass* Class, const FString& MethodName);

	TSharedPtr<FCSharpClassInfo> FindOrAddClassInfo(UClass* Class);
	TSharedPtr<FCSharpClassInfo> FindClassInfo(FName ClassName) const;

	FGCHandle* CreateNewManagedObject(UObject* Object);
	FGCHandle* FindManagedObject(UObject* Object);

private:

	bool ProcessMetaData(const FString& FilePath);

	void RemoveManagedObject(const UObjectBase* Object);

	TMap<FName, TSharedPtr<FCSharpClassInfo>> ManagedClasses;
	TMap<FName, TSharedPtr<FCSharpStructInfo>> ManagedStructs;
	TMap<FName, TSharedPtr<FCSharpEnumInfo>> ManagedEnums;
	TMap<FName, TSharedPtr<FCSharpInterfaceInfo>> ManagedInterfaces;

	TArray<TSharedPtr<FGCHandle>> AllocatedHandles;
	TMap<const UObjectBase*, FGCHandle> UnmanagedToManagedMap;
	
	FGCHandle Assembly;
	FString AssemblyPath;
	FName AssemblyName;

	
};
