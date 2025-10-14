﻿#pragma once

#include "CSFieldName.h"
#include "CSManagedGCHandle.h"
#include "UnrealSharpCore.h"
#include "Logging/StructuredLog.h"
#include "TypeInfo/CSFieldType.h"
#include "Utils/CSClassUtilities.h"
#include "CSAssembly.generated.h"

#if !defined(_WIN32)
#define __stdcall
#endif

struct FCSManagedMethod;
class UCSClass;

/**
 * Represents a managed assembly.
 * This class is responsible for loading and unloading the assembly, as well as managing all types that are defined in the C# assembly.
 */
UCLASS()
class UCSAssembly : public UObject
{
	GENERATED_BODY()
public:
	void InitializeAssembly(const FStringView InAssemblyPath);

	UNREALSHARPCORE_API bool LoadAssembly(bool bIsCollectible = true);
	UNREALSHARPCORE_API bool UnloadAssembly();
	UNREALSHARPCORE_API bool IsValidAssembly() const { return ManagedAssemblyHandle.IsValid() && !ManagedAssemblyHandle->IsNull(); }

	UFUNCTION(meta = (ScriptMethod))
	FName GetAssemblyName() const { return AssemblyName; }

	UFUNCTION(meta = (ScriptMethod))
	const FString& GetAssemblyPath() const { return AssemblyPath; }

	bool IsLoading() const { return bIsLoading; }

	TSharedPtr<FGCHandle> TryFindTypeHandle(const FCSFieldName& FieldName);
	
	TSharedPtr<FGCHandle> RegisterTypeHandle(const FCSFieldName& FieldName, uint8* TypeHandle)
	{
		TSharedPtr<FGCHandle> AllocatedHandle = MakeShared<FGCHandle>(TypeHandle, GCHandleType::WeakHandle);
		AllocatedManagedHandles.Add(AllocatedHandle);
		ManagedClassHandles.Add(FieldName, AllocatedHandle);
		return AllocatedHandle;
	}
	
	TSharedPtr<FGCHandle> GetManagedMethod(const TSharedPtr<FGCHandle>& TypeHandle, const FString& MethodName);
	
	TSharedPtr<FCSManagedTypeInfo> FindOrAddTypeInfo(UClass* Field)
	{
		if (ICSManagedTypeInterface* ManagedClass = FCSClassUtilities::GetManagedType(Field))
		{
			return ManagedClass->GetManagedTypeInfo();
		}	
	
		FCSFieldName FieldName(Field);
		return FindOrAddTypeInfo(FieldName);
	}
	
	TSharedPtr<FCSManagedTypeInfo> FindOrAddTypeInfo(const FCSFieldName& ClassName)
	{
		TRACE_CPUPROFILER_EVENT_SCOPE(UCSAssembly::FindOrAddClassInfo);

		TSharedPtr<FCSManagedTypeInfo>& TypeInfo = AllTypes.FindOrAdd(ClassName);

		// Native types are populated on the go when they are needed for managed code execution.
		if (!TypeInfo.IsValid())
		{
			UField* Field = TryFindField(ClassName);

			if (!IsValid(Field))
			{
				UE_LOGFMT(LogUnrealSharp, Error, "Failed to find native class: {0}", *ClassName.GetName());
				return nullptr;
			}

			TypeInfo = MakeShared<FCSManagedTypeInfo>(Field, this);
		}

		return TypeInfo;
	}

	template<typename T = FCSManagedTypeInfo>
	TSharedPtr<T> FindTypeInfo(const FCSFieldName& FieldName) const
	{
		TRACE_CPUPROFILER_EVENT_SCOPE(UCSAssembly::FindClassInfo);
		static_assert(TIsDerivedFrom<T, FCSManagedTypeInfo>::Value, "T must be a FCSManagedTypeInfo-derived type.");

		const TSharedPtr<FCSManagedTypeInfo>* TypeInfo = AllTypes.Find(FieldName);
		if (TypeInfo && TypeInfo->IsValid())
		{
			return StaticCastSharedPtr<T>(*TypeInfo);
		}

		return nullptr;
	}

	template<typename T = UField>
	T* FindType(const FCSFieldName& FieldName) const
	{
		TRACE_CPUPROFILER_EVENT_SCOPE(UCSAssembly::FindType);
		static_assert(TIsDerivedFrom<T, UField>::Value, "T must be a UField-derived type.");

		TSharedPtr<FCSManagedTypeInfo> TypeInfo = AllTypes.FindRef(FieldName);
		if (TypeInfo.IsValid())
		{
			return Cast<T>(TypeInfo->GetOrBuildType());
		}

		return TryFindField<T>(FieldName);
	}

	TSharedPtr<FCSManagedTypeInfo> TryRegisterType(TCHAR* InFieldName,
		TCHAR* InNamespace,
		int64 LastModifiedTime,
		ECSFieldType FieldType,
		uint8* TypeHandle,
		bool& NeedsRebuild);

	// Creates a C# counterpart for the given UObject.
	TSharedPtr<FGCHandle> CreateManagedObject(const UObject* Object);
	TSharedPtr<FGCHandle> FindOrCreateManagedInterfaceWrapper(UObject* Object, UClass* InterfaceClass);

	TSharedPtr<const FGCHandle> GetManagedAssemblyHandle() const { return ManagedAssemblyHandle; }
	
	void AddTypeToRebuild(const TSharedPtr<FCSManagedTypeInfo>& TypeInfo) { PendingRebuild.AddUnique(TypeInfo); }

private:

	template<typename T = UField>
	T* TryFindField(const FCSFieldName FieldName) const
	{
		TRACE_CPUPROFILER_EVENT_SCOPE(UCSAssembly::TryFindField);
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
	TMap<FCSFieldName, TSharedPtr<FCSManagedTypeInfo>> AllTypes;
	TArray<TSharedPtr<FCSManagedTypeInfo>> PendingRebuild;
	
	// All handles allocated by this assembly. Handles to types, methods, objects.
	TArray<TSharedPtr<FGCHandle>> AllocatedManagedHandles;

	// Handles to all allocated UTypes (UClass/UStruct, etc) that are defined in this assembly.
	TMap<FCSFieldName, TSharedPtr<FGCHandle>> ManagedClassHandles;

	// Handle to the Assembly object in C#.
	TSharedPtr<FGCHandle> ManagedAssemblyHandle;

	// Full path to the assembly file.
	FString AssemblyPath;

	// Assembly file name without the path.
	FName AssemblyName;
	
	bool bIsLoading = false;
};