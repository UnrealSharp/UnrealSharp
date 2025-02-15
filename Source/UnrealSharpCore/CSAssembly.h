#pragma once

#include "CSManagedGCHandle.h"
#include "TypeGenerator/Register/MetaData/CSTypeReferenceMetaData.h"

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
	using LoadPluginCallback = GCHandleIntPtr(__stdcall*)(const TCHAR*, bool);
	using UnloadPluginCallback = bool(__stdcall*)(const TCHAR*);
	
	LoadPluginCallback LoadPlugin = nullptr;
	UnloadPluginCallback UnloadPlugin = nullptr;
};

struct FPendingClasses
{
	TSet<FCSharpClassInfo*> Classes;
};

struct FCSAssembly : public TSharedFromThis<FCSAssembly>, public FUObjectArray::FUObjectDeleteListener
{
	FCSAssembly(const FString& InAssemblyPath);

	UNREALSHARPCORE_API bool Load(bool bIsCollectible = true);
	UNREALSHARPCORE_API bool Unload();
	
	bool IsValid() const;

	UPackage* GetPackage(const FName Namespace);

	const GCHandleIntPtr& GetAssemblyHandle() const { return Assembly.Handle; }
	const FName& GetAssemblyName() const { return AssemblyName; }
	const FString& GetAssemblyPath() const { return AssemblyPath; }

	TWeakPtr<FGCHandle> TryFindTypeHandle(const FName& Namespace, const FName& TypeName);
	TWeakPtr<FGCHandle> TryFindTypeHandle(const UClass* Class);

	bool ContainsClass(const UClass* Class) const;
	
	TWeakPtr<FGCHandle> GetMethodHandle(const UCSClass* Class, const FString& MethodName);

	TSharedPtr<FCSharpClassInfo> FindOrAddClassInfo(const UClass* Class);
	TSharedPtr<FCSharpClassInfo> FindOrAddClassInfo(FName ClassName);
	TSharedPtr<FCSharpClassInfo> FindClassInfo(FName ClassName) const;

	TSharedPtr<FCSharpStructInfo> FindStructInfo(FName StructName) const;
	TSharedPtr<FCSharpEnumInfo> FindEnumInfo(FName EnumName) const;
	TSharedPtr<FCSharpInterfaceInfo> FindInterfaceInfo(FName InterfaceName) const;
	
	UClass* FindClass(FName ClassName) const;
	UScriptStruct* FindStruct(FName StructName) const;
	UEnum* FindEnum(FName EnumName) const;
	UClass* FindInterface(FName InterfaceName) const;
	
	FGCHandle* CreateNewManagedObject(UObject* Object);
	void RemoveManagedObject(const UObjectBase* Object);
	
	FGCHandle FindManagedObject(UObject* Object);

	void AddPendingClass(const FCSTypeReferenceMetaData& ParentClass, FCSharpClassInfo* NewClass);

private:

	bool ProcessMetaData(const FString& FilePath);

	void OnModulesChanged(FName InModuleName, EModuleChangeReason InModuleChangeReason);
	void OnEnginePreExit();

	// UObjectArray listener interface
	virtual void NotifyUObjectDeleted(const class UObjectBase *Object, int32 Index) override;
	virtual void OnUObjectArrayShutdown() override;
	// End of interface

	TMap<FName, TSharedPtr<FCSharpClassInfo>> Classes;
	TMap<FName, TSharedPtr<FCSharpStructInfo>> Structs;
	TMap<FName, TSharedPtr<FCSharpEnumInfo>> Enums;
	TMap<FName, TSharedPtr<FCSharpInterfaceInfo>> Interfaces;
	
	TArray<TSharedPtr<FGCHandle>> AllHandles;
	TMap<FName, TSharedPtr<FGCHandle>> ClassHandles;
	TMap<const UObjectBase*, FGCHandle> ObjectHandles;
	
	TMap<FCSTypeReferenceMetaData, FPendingClasses> PendingClasses;
	TArray<UPackage*> AssemblyPackages;
	
	FGCHandle Assembly;
	FString AssemblyPath;
	FName AssemblyName;
};
