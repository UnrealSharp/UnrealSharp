#pragma once

#include "CSManagedGCHandle.h"
#include "UnrealSharpCore.h"
#include "Logging/StructuredLog.h"
#include "TypeGenerator/Register/MetaData/CSTypeReferenceMetaData.h"
#include "Utils/CSClassUtilities.h"
#include "CSAssembly.generated.h"

#if !defined(_WIN32)
#define __stdcall
#endif

struct FCSClassInfo;
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
	void SetAssemblyPath(const FStringView InAssemblyPath);

	UNREALSHARPCORE_API bool LoadAssembly(bool bIsCollectible = true);
	UNREALSHARPCORE_API bool UnloadAssembly();
	UNREALSHARPCORE_API bool IsValidAssembly() const { return ManagedAssemblyHandle.IsValid() && !ManagedAssemblyHandle->IsNull(); }

	FName GetAssemblyName() const { return AssemblyName; }
	const FString& GetAssemblyPath() const { return AssemblyPath; }

	bool IsLoading() const { return bIsLoading; }

	TSharedPtr<FGCHandle> TryFindTypeHandle(const FCSFieldName& FieldName);
	TSharedPtr<FGCHandle> GetManagedMethod(const TSharedPtr<FGCHandle>& TypeHandle, const FString& MethodName);

	template<typename T = FCSManagedTypeInfo>
	TSharedPtr<T> FindOrAddTypeInfo(UClass* Field)
	{
		if (ICSManagedTypeInterface* ManagedClass = FCSClassUtilities::GetManagedType(Field))
		{
			return ManagedClass->GetManagedTypeInfo<T>();
		}	
	
		FCSFieldName FieldName(Field);
		return FindOrAddTypeInfo<T>(FieldName);
	}

	template<typename T = FCSManagedTypeInfo>
	TSharedPtr<T> FindOrAddTypeInfo(const FCSFieldName& ClassName)
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

			TSharedPtr<FGCHandle> TypeHandle = TryFindTypeHandle(Field);

			if (!TypeHandle.IsValid())
			{
				UE_LOGFMT(LogUnrealSharp, Error, "Failed to find type handle for native class: {0}", *ClassName.GetName());
				return nullptr;
			}

			TypeInfo = MakeShared<FCSManagedTypeInfo>(Field, this, TypeHandle);
		}

		if constexpr (std::is_same_v<T, FCSManagedTypeInfo>)
		{
			return TypeInfo;
		}
		else
		{
			return StaticCastSharedPtr<T>(TypeInfo);
		}
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
			return Cast<T>(TypeInfo->InitializeBuilder());
		}

		return TryFindField<T>(FieldName);
	}

	// Creates a C# counterpart for the given UObject.
	TSharedPtr<FGCHandle> CreateManagedObject(const UObject* Object);
	TSharedPtr<FGCHandle> FindOrCreateManagedInterfaceWrapper(UObject* Object, UClass* InterfaceClass);

	// Add a class that is waiting for its parent class to be loaded before it can be created.
	void AddPendingClass(const FCSTypeReferenceMetaData& ParentClass, FCSClassInfo* NewClass);

	TSharedPtr<const FGCHandle> GetManagedAssemblyHandle() const { return ManagedAssemblyHandle; }

private:
	
	bool ProcessMetadata();
	void BuildUnrealTypes();

	void OnModulesChanged(FName InModuleName, EModuleChangeReason InModuleChangeReason);

	template<typename T = UField>
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
	TMap<FCSFieldName, TSharedPtr<FCSManagedTypeInfo>> AllTypes;
	
	// All handles allocated by this assembly. Handles to types, methods, objects.
	TArray<TSharedPtr<FGCHandle>> AllocatedManagedHandles;

	// Handles to all allocated UTypes (UClass/UStruct, etc) that are defined in this assembly.
	TMap<FCSFieldName, TSharedPtr<FGCHandle>> ManagedClassHandles;

	// Pending classes that are waiting for their parent class to be loaded by the engine.
	TMap<FCSTypeReferenceMetaData, TSet<FCSClassInfo*>> PendingClasses;

	// Handle to the Assembly object in C#.
	TSharedPtr<FGCHandle> ManagedAssemblyHandle;

	// Full path to the assembly file.
	FString AssemblyPath;

	// Assembly file name without the path.
	FName AssemblyName;

	bool bIsLoading = false;
};