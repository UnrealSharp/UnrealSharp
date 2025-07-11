#pragma once

#include "CSManagedGCHandle.h"
#include "UnrealSharpCore.h"
#include "Logging/StructuredLog.h"
#include "TypeGenerator/Register/MetaData/CSTypeReferenceMetaData.h"
#include "Utils/CSClassUtilities.h"

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
struct FCSAssembly final : TSharedFromThis<FCSAssembly>
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
	TSharedPtr<FGCHandle> GetManagedMethod(const TSharedPtr<FGCHandle>& TypeHandle, const FString& MethodName);
	
	TSharedPtr<FCSClassInfo> FindOrAddClassInfo(UClass* Class)
	{
		if (UCSClass* ManagedClass = FCSClassUtilities::GetFirstManagedClass(Class))
		{
			return ManagedClass->GetTypeInfo();
		}
	
		FCSFieldName FieldName(Class);
		return FindOrAddClassInfo(FieldName);
	}
	
	TSharedPtr<FCSClassInfo> FindOrAddClassInfo(const FCSFieldName& ClassName);
	
	TSharedPtr<FCSClassInfo> FindClassInfo(const FCSFieldName& ClassName) const { return Classes.FindRef(ClassName); }
	TSharedPtr<FCSStructInfo> FindStructInfo(const FCSFieldName& StructName) const { return Structs.FindRef(StructName); }
	TSharedPtr<FCSEnumInfo> FindEnumInfo(const FCSFieldName& EnumName) const { return Enums.FindRef(EnumName); }
	TSharedPtr<FCSInterfaceInfo> FindInterfaceInfo(const FCSFieldName& InterfaceName) const { return Interfaces.FindRef(InterfaceName); }
	
	UClass* FindClass(const FCSFieldName& FieldName) const;
	UScriptStruct* FindStruct(const FCSFieldName& StructName) const;
	UEnum* FindEnum(const FCSFieldName& EnumName) const;
	UClass* FindInterface(const FCSFieldName& InterfaceName) const;
	UDelegateFunction* FindDelegate(const FCSFieldName& DelegateName) const;

	// Creates a C# counterpart for the given UObject.
	TSharedPtr<FGCHandle> CreateManagedObject(const UObject* Object);

	// Add a class that is waiting for its parent class to be loaded before it can be created.
	void AddPendingClass(const FCSTypeReferenceMetaData& ParentClass, FCSClassInfo* NewClass);

	TSharedPtr<const FGCHandle> GetManagedAssemblyHandle() const { return ManagedAssemblyHandle; }

private:
	
	bool ProcessMetadata();
	void BuildUnrealTypes();

	void OnModulesChanged(FName InModuleName, EModuleChangeReason InModuleChangeReason);

	template<typename Field, typename InfoType>
	Field* FindFieldFromInfo(const FCSFieldName& EnumName, const TMap<FCSFieldName, TSharedPtr<InfoType>>& Map) const
	{
		Field* FoundField;
		if (TSharedPtr<InfoType> InfoPtr = Map.FindRef(EnumName))
		{
			FoundField = InfoPtr->InitializeBuilder();
		}
		else
		{
			FoundField = TryFindField<Field>(EnumName);
		}

		if (!FoundField)
		{
			UE_LOGFMT(LogUnrealSharp, Fatal, "Failed to find field: {0}", *EnumName.GetName());
			return nullptr;
		}
	
		return FoundField;
	}

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

	// Pending classes that are waiting for their parent class to be loaded by the engine.
	TMap<FCSTypeReferenceMetaData, TSet<FCSClassInfo*>> PendingClasses;

	// Handle to the Assembly object in C#.
	TSharedPtr<FGCHandle> ManagedAssemblyHandle;
	
	FString AssemblyPath;
	FName AssemblyName;

	bool bIsLoading = false;
};