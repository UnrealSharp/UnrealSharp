#pragma once

#include "CSManagedGCHandle.h"
#include "UnrealSharpCore.h"
#include "Logging/StructuredLog.h"
#include "TypeGenerator/Register/MetaData/CSTypeReferenceMetaData.h"

#if !defined(_WIN32)
#define __stdcall
#endif

struct FCSDelegateInfo;
struct FCSManagedMethod;
class UCSClass;
struct FCSClassInfo;
struct FCSInterfaceInfo;
struct FCSEnumInfo;
struct FCSStructInfo;

/**
 * Represents a managed assembly.
 * This class is responsible for loading and unloading the assembly, as well as managing all types that are defined in the C# assembly.
 */
struct FCSAssembly final : TSharedFromThis<FCSAssembly>, FUObjectArray::FUObjectDeleteListener
{
	explicit FCSAssembly(const FString& AssemblyPath);

	UNREALSHARPCORE_API bool LoadAssembly(bool bIsCollectible = true);
	UNREALSHARPCORE_API bool UnloadAssembly();
	UNREALSHARPCORE_API bool IsValidAssembly() const { return ManagedAssemblyHandle.IsValid() && !ManagedAssemblyHandle->IsNull(); }

	static UPackage* GetPackage(const FCSNamespace Namespace);

	FName GetAssemblyName() const { return AssemblyName; }
	const FString& GetAssemblyPath() const { return AssemblyPath; }

	bool IsLoading() const { return bIsLoading; }

	TSharedPtr<FGCHandle> TryFindTypeHandle(const FCSFieldName& FieldName);
	TSharedPtr<FGCHandle> TryFindTypeHandle(UClass* Class);

	FCSManagedMethod GetManagedMethod(const TSharedPtr<FGCHandle>& TypeHandle, const FString& MethodName);
	FCSManagedMethod GetManagedMethod(const UCSClass* Class, const FString& MethodName);
	
	TSharedPtr<FCSClassInfo> FindOrAddClassInfo(UClass* Class);
	TSharedPtr<FCSClassInfo> FindOrAddClassInfo(const FCSFieldName& ClassName);
	
	TSharedPtr<FCSClassInfo> FindClassInfo(const FCSFieldName& ClassName) const;

	TSharedPtr<FCSStructInfo> FindStructInfo(const FCSFieldName& StructName) const;
	TSharedPtr<FCSEnumInfo> FindEnumInfo(const FCSFieldName& EnumName) const;
	TSharedPtr<FCSInterfaceInfo> FindInterfaceInfo(const FCSFieldName& InterfaceName) const;
	
	UClass* FindClass(const FCSFieldName& FieldName) const;
	UScriptStruct* FindStruct(const FCSFieldName& StructName) const;
	UEnum* FindEnum(const FCSFieldName& EnumName) const;
	UClass* FindInterface(const FCSFieldName& InterfaceName) const;
	UDelegateFunction* FindDelegate(const FCSFieldName& DelegateName) const;

	// Creates a C# counterpart for the given UObject.
	TSharedPtr<FGCHandle> FindOrCreateManagedObject(UObject* Object);

	// Removes the C# counterpart for the given UObject, if it exists.
	void RemoveManagedObject(const UObjectBase* Object);

	// Add a class that is waiting for its parent class to be loaded before it can be created.
	void AddPendingClass(const FCSTypeReferenceMetaData& ParentClass, FCSClassInfo* NewClass);

private:

	bool bIsLoading = false;
	
	bool ProcessMetadata();

	void BuildUnrealTypes();

	void OnModulesChanged(FName InModuleName, EModuleChangeReason InModuleChangeReason);
	void OnEnginePreExit();

	template<typename T>
	T* TryFindField(const FCSFieldName FieldName) const
	{
		TRACE_CPUPROFILER_EVENT_SCOPE(UCSManager::TryFindField);
		static_assert(TIsDerivedFrom<T, UObject>::Value, "T must be a UObject-derived type.");

		if (!FieldName.IsValid())
		{
			UE_LOGFMT(LogUnrealSharp, Warning, "Invalid field name: {0}", *FieldName.GetName());
			return nullptr;
		}

		UPackage* Package = FieldName.GetPackage();
		if (!IsValid(Package))
		{
			UE_LOGFMT(LogUnrealSharp, Warning, "Failed to find package for field: {0}", *FieldName.GetName());
			return nullptr;
		}

		return FindObject<T>(Package, *FieldName.GetName());
	}

	// UObjectArray listener interface
	virtual void NotifyUObjectDeleted(const UObjectBase* Object, int32 Index) override;
	virtual void OnUObjectArrayShutdown() override;
	// End of interface

	// All Unreal types that are defined in this assembly.
	TMap<FCSFieldName, TSharedPtr<FCSClassInfo>> Classes;
	TMap<FCSFieldName, TSharedPtr<FCSStructInfo>> Structs;
	TMap<FCSFieldName, TSharedPtr<FCSEnumInfo>> Enums;
	TMap<FCSFieldName, TSharedPtr<FCSInterfaceInfo>> Interfaces;
	TMap<FCSFieldName, TSharedPtr<FCSDelegateInfo>> Delegates;
	
	// All handles allocated by this assembly. Handles to types, methods, objects.
	TArray<TSharedPtr<FGCHandle>> AllocatedManagedHandles;

	// Handles to all UClasses types that are defined in this assembly.
	TMap<FCSFieldName, TSharedPtr<FGCHandle>> ManagedClassHandles;

	// Handles to all active UObjects that has a C# counterpart.
	TMap<uint32, TSharedPtr<FGCHandle>> ManagedObjectHandles;

	// Pending classes that are waiting for their parent class to be loaded by the engine.
	TMap<FCSTypeReferenceMetaData, TSet<FCSClassInfo*>> PendingClasses;

	// Handle to the Assembly object in C#.
	TSharedPtr<FGCHandle> ManagedAssemblyHandle;
	
	FString AssemblyPath;
	FName AssemblyName;
};