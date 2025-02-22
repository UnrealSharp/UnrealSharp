#pragma once

#include "CSManagedGCHandle.h"
#include "TypeGenerator/Register/MetaData/CSTypeReferenceMetaData.h"

#if !defined(_WIN32)
#define __stdcall
#endif

struct FCSManagedMethod;
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

struct FCSAssembly : TSharedFromThis<FCSAssembly>, FUObjectArray::FUObjectDeleteListener
{
	FCSAssembly(const FString& AssemblyPath);

	UNREALSHARPCORE_API bool LoadAssembly(bool bIsCollectible = true);
	UNREALSHARPCORE_API bool UnloadAssembly();
	
	UNREALSHARPCORE_API bool IsValid() const { return !Assembly.IsNull(); }
	UNREALSHARPCORE_API FGCHandle GetAssemblyHandle() { return Assembly; }

	static UPackage* GetPackage(const FCSNamespace Namespace);

	const GCHandleIntPtr& GetAssemblyHandle() const { return Assembly.Handle; }
	const FName& GetAssemblyName() const { return AssemblyName; }
	const FString& GetAssemblyPath() const { return AssemblyPath; }

	TWeakPtr<FGCHandle> TryFindTypeHandle(const FCSFieldName& FieldName);
	TWeakPtr<FGCHandle> TryFindTypeHandle(const UClass* Class);

	FCSManagedMethod GetManagedMethod(const TSharedPtr<FGCHandle>& TypeHandle, const FString& MethodName);
	FCSManagedMethod GetManagedMethod(const UCSClass* Class, const FString& MethodName);

	TSharedPtr<const FCSharpClassInfo> FindOrAddClassInfo(UClass* Class);
	TSharedPtr<FCSharpClassInfo> FindOrAddClassInfo(const FCSFieldName& ClassName);
	TSharedPtr<FCSharpClassInfo> FindClassInfo(const FCSFieldName& ClassName) const;

	TSharedPtr<FCSharpStructInfo> FindStructInfo(const FCSFieldName& StructName) const;
	TSharedPtr<FCSharpEnumInfo> FindEnumInfo(const FCSFieldName& EnumName) const;
	TSharedPtr<FCSharpInterfaceInfo> FindInterfaceInfo(const FCSFieldName& InterfaceName) const;
	
	UClass* FindClass(const FCSFieldName& FieldName) const;
	UScriptStruct* FindStruct(const FCSFieldName& StructName) const;
	UEnum* FindEnum(const FCSFieldName& EnumName) const;
	UClass* FindInterface(const FCSFieldName& InterfaceName) const;
	
	FGCHandle* CreateNewManagedObject(UObject* Object);
	void RemoveManagedObject(const UObjectBase* Object);
	
	FGCHandle FindManagedObject(UObject* Object);

	void AddPendingClass(const FCSTypeReferenceMetaData& ParentClass, FCSharpClassInfo* NewClass);

private:
	
	bool ProcessMetadata();
	bool ProcessMetaData_Internal(const FString& FilePath);

	void BuildUnrealTypes();

	void OnModulesChanged(FName InModuleName, EModuleChangeReason InModuleChangeReason);
	void OnEnginePreExit();

	template<typename T>
	T* TryFindField(FCSFieldName FieldName) const
	{
		UPackage* Package = FieldName.GetPackage();

		if (!Package)
		{
			return nullptr;
		}

		return FindObject<T>(Package, *FieldName.GetNameString());
	}

	// UObjectArray listener interface
	virtual void NotifyUObjectDeleted(const class UObjectBase *Object, int32 Index) override;
	virtual void OnUObjectArrayShutdown() override;
	// End of interface

	TMap<FCSFieldName, TSharedPtr<FCSharpClassInfo>> Classes;
	TMap<FCSFieldName, TSharedPtr<FCSharpStructInfo>> Structs;
	TMap<FCSFieldName, TSharedPtr<FCSharpEnumInfo>> Enums;
	TMap<FCSFieldName, TSharedPtr<FCSharpInterfaceInfo>> Interfaces;
	
	TArray<TSharedPtr<FGCHandle>> AllHandles;
	TMap<FCSFieldName, TSharedPtr<FGCHandle>> ClassHandles;
	TMap<const UObjectBase*, FGCHandle> ObjectHandles;
	
	TMap<FCSTypeReferenceMetaData, TSet<FCSharpClassInfo*>> PendingClasses;
	TArray<UPackage*> AssemblyPackages;
	
	FGCHandle Assembly;
	FString AssemblyPath;
	FName AssemblyName;
};