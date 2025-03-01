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

/**
 * Represents a managed assembly.
 * This class is responsible for loading and unloading the assembly, as well as managing all types that are defined in the C# assembly.
 */
struct FCSAssembly final : TSharedFromThis<FCSAssembly>, FUObjectArray::FUObjectDeleteListener
{
	explicit FCSAssembly(const FString& AssemblyPath);

	UNREALSHARPCORE_API bool LoadAssembly(bool bIsCollectible = true);
	UNREALSHARPCORE_API bool UnloadAssembly();
	UNREALSHARPCORE_API bool IsValidAssembly() const { return AssemblyHandle.IsValid() && !AssemblyHandle->IsNull(); }

	static UPackage* GetPackage(const FCSNamespace Namespace);

	FName GetAssemblyName() const { return AssemblyName; }
	const FString& GetAssemblyPath() const { return AssemblyPath; }

	TSharedPtr<FGCHandle> TryFindTypeHandle(const FCSFieldName& FieldName);
	TSharedPtr<FGCHandle> TryFindTypeHandle(const UClass* Class);

	FCSManagedMethod GetManagedMethod(const TSharedPtr<FGCHandle>& TypeHandle, const FString& MethodName);
	FCSManagedMethod GetManagedMethod(const UCSClass* Class, const FString& MethodName);
	
	TSharedPtr<FCSharpClassInfo> FindOrAddClassInfo(UClass* Class);
	TSharedPtr<FCSharpClassInfo> FindOrAddClassInfo(const FCSFieldName& ClassName);
	
	TSharedPtr<FCSharpClassInfo> FindClassInfo(const FCSFieldName& ClassName) const;

	TSharedPtr<FCSharpStructInfo> FindStructInfo(const FCSFieldName& StructName) const;
	TSharedPtr<FCSharpEnumInfo> FindEnumInfo(const FCSFieldName& EnumName) const;
	TSharedPtr<FCSharpInterfaceInfo> FindInterfaceInfo(const FCSFieldName& InterfaceName) const;
	
	UClass* FindClass(const FCSFieldName& FieldName) const;
	UScriptStruct* FindStruct(const FCSFieldName& StructName) const;
	UEnum* FindEnum(const FCSFieldName& EnumName) const;
	UClass* FindInterface(const FCSFieldName& InterfaceName) const;

	// Creates a C# counterpart for the given UObject.
	FGCHandle* CreateManagedObject(UObject* Object);

	// Removes the C# counterpart for the given UObject, if it exists.
	void RemoveManagedObject(const UObjectBase* Object);

	// Finds the object handle for the given UObject.
	FGCHandle FindManagedObject(UObject* Object);

	// Add a class that is waiting for its parent class to be loaded before it can be created.
	void AddPendingClass(const FCSTypeReferenceMetaData& ParentClass, FCSharpClassInfo* NewClass);

private:
	
	bool ProcessMetadata();

	void BuildUnrealTypes();

	void OnModulesChanged(FName InModuleName, EModuleChangeReason InModuleChangeReason);
	void OnEnginePreExit();

	template<typename T>
	T* TryFindField(const FCSFieldName FieldName) const
	{
		UPackage* Package = FieldName.GetPackage();

		if (!Package)
		{
			return nullptr;
		}

		return FindObject<T>(Package, *FieldName.GetName());
	}

	// UObjectArray listener interface
	virtual void NotifyUObjectDeleted(const UObjectBase* Object, int32 Index) override;
	virtual void OnUObjectArrayShutdown() override;
	// End of interface

	// All Unreal types that are defined in this assembly.
	TMap<FCSFieldName, TSharedPtr<FCSharpClassInfo>> Classes;
	TMap<FCSFieldName, TSharedPtr<FCSharpStructInfo>> Structs;
	TMap<FCSFieldName, TSharedPtr<FCSharpEnumInfo>> Enums;
	TMap<FCSFieldName, TSharedPtr<FCSharpInterfaceInfo>> Interfaces;
	
	// All handles allocated by this assembly. Handles to types, methods, objects.
	TArray<TSharedPtr<FGCHandle>> AllocatedHandles;

	// Handles to all UClasses types that are defined in this assembly.
	TMap<FCSFieldName, TSharedPtr<FGCHandle>> ClassHandles;

	// Handles to all active UObjects that has a C# counterpart.
	TMap<const UObjectBase*, FGCHandle> ObjectHandles;

	// Pending classes that are waiting for their parent class to be loaded by the engine.
	TMap<FCSTypeReferenceMetaData, TSet<FCSharpClassInfo*>> PendingClasses;

	// Handle to the Assembly object in C#.
	TSharedPtr<FGCHandle> AssemblyHandle;
	
	FString AssemblyPath;
	FName AssemblyName;
};