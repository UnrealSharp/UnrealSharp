#pragma once

#include "CSFieldName.h"
#include "CSManagedGCHandle.h"
#include "UnrealSharpCore.h"
#include "Logging/StructuredLog.h"
#include "CSFieldType.h"
#include "Misc/Paths.h"
#include "Utilities/CSClassUtilities.h"
#include "Utilities/CSUtilities.h"
#include "CSManagedAssembly.generated.h"

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
class UCSManagedAssembly : public UObject
{
	GENERATED_BODY()
public:
	void InitializeManagedAssembly(const FStringView InAssemblyPath);

	UNREALSHARPCORE_API bool LoadManagedAssembly(bool bIsCollectible = true);
	UNREALSHARPCORE_API bool UnloadManagedAssembly();

	UNREALSHARPCORE_API bool IsValidAssembly() const { return AssemblyGCHandle.IsValid() && !AssemblyGCHandle->IsNull(); }
	
	UNREALSHARPCORE_API FName GetAssemblyName() const { return AssemblyName; }
	
	UNREALSHARPCORE_API const FString& GetAssemblyFilePath() const { return AssemblyFilePath; }
	UNREALSHARPCORE_API FString GetAssemblyFileName() const { return FPaths::GetCleanFilename(AssemblyFilePath); }
	
	UNREALSHARPCORE_API bool IsAssemblyLoaded() const { return bIsLoading; }
	
	UNREALSHARPCORE_API const TMap<FCSFieldName, TSharedPtr<FCSManagedTypeDefinition>>& GetDefinedManagedTypes() const { return DefinedManagedTypes; }
	
#if WITH_EDITOR
	UNREALSHARPCORE_API void AddDependentAssembly(UCSManagedAssembly* DependencyAssembly) { DependentAssemblies.Add(DependencyAssembly); }
	UNREALSHARPCORE_API const TArray<UCSManagedAssembly*>& GetDependentAssemblies() const { return DependentAssemblies; }
#endif

	TSharedPtr<FGCHandle> FindTypeHandle(const FCSFieldName& FieldName);
	TSharedPtr<FGCHandle> AddTypeHandle(const FCSFieldName& FieldName, uint8* TypeHandle)
	{
		TSharedPtr<FGCHandle> AllocatedHandle = MakeShared<FGCHandle>(TypeHandle, GCHandleType::WeakHandle);
		AllocatedGCHandles.Add(AllocatedHandle);
		ManagedTypeGCHandles.Add(FieldName, AllocatedHandle);
		return AllocatedHandle;
	}
	
	TSharedPtr<FGCHandle> GetManagedMethod(const TSharedPtr<FGCHandle>& TypeHandle, const FString& MethodName);
	
	TSharedPtr<FCSManagedTypeDefinition> FindOrAddManagedTypeDefinition(UClass* Field)
	{
		if (ICSManagedTypeInterface* ManagedClass = FCSClassUtilities::GetManagedType(Field))
		{
			return ManagedClass->GetManagedTypeDefinition();
		}	
	
		FCSFieldName FieldName(Field);
		return FindOrAddManagedTypeDefinition(FieldName);
	}
	
	TSharedPtr<FCSManagedTypeDefinition> FindOrAddManagedTypeDefinition(const FCSFieldName& ClassName)
	{
		TRACE_CPUPROFILER_EVENT_SCOPE(UCSAssembly::FindOrAddManagedTypeDefinition);

		TSharedPtr<FCSManagedTypeDefinition>& ManagedTypeDefinition = DefinedManagedTypes.FindOrAdd(ClassName);

		// Native types are populated on the go when they are needed for managed code execution.
		if (!ManagedTypeDefinition.IsValid())
		{
			UField* Field = FCSUtilities::FindField<UField>(ClassName);

			if (!IsValid(Field))
			{
				UE_LOGFMT(LogUnrealSharp, Error, "Failed to find native class: {0}", *ClassName.GetName());
				return nullptr;
			}

			ManagedTypeDefinition = FCSManagedTypeDefinition::CreateFromNativeField(Field, this);
		}

		return ManagedTypeDefinition;
	}
	
	UNREALSHARPCORE_API TSharedPtr<FCSManagedTypeDefinition> FindManagedTypeDefinition(const FCSFieldName& FieldName) const
	{
		TRACE_CPUPROFILER_EVENT_SCOPE(UCSAssembly::FindManagedTypeDefinition);

		const TSharedPtr<FCSManagedTypeDefinition>* ManagedTypeDefinition = DefinedManagedTypes.Find(FieldName);
		
		if (!ManagedTypeDefinition || !ManagedTypeDefinition->IsValid())
		{
			return nullptr;
		}

		return *ManagedTypeDefinition;
	}

	template<typename T = UField>
	T* ResolveUField(const FCSFieldName& FieldName) const
	{
		TRACE_CPUPROFILER_EVENT_SCOPE(UCSAssembly::FindType);
		static_assert(TIsDerivedFrom<T, UField>::Value, "T must be a UField-derived type.");

		TSharedPtr<FCSManagedTypeDefinition> ManagedTypeDefinition = FindManagedTypeDefinition(FieldName);
		if (ManagedTypeDefinition.IsValid())
		{
			return Cast<T>(ManagedTypeDefinition->CompileAndGetDefinitionField());
		}
		
		return FCSUtilities::FindField<T>(FieldName);
	}

	TSharedPtr<FCSManagedTypeDefinition> RegisterManagedType(char* InFieldName, char* InNamespace, ECSFieldType FieldType, uint8* TypeGCHandle, const char* RawJsonString);

	// Creates a C# counterpart for the given UObject.
	TSharedPtr<FGCHandle> CreateManagedObjectFromNative(const UObject* Object);
	TSharedPtr<FGCHandle> CreateManagedObjectFromNative(const UObject* Object, const TSharedPtr<FGCHandle>& TypeGCHandle);
	
	TSharedPtr<FGCHandle> GetOrCreateManagedInterface(UObject* Object, UClass* InterfaceClass);

	TSharedPtr<const FGCHandle> GetManagedAssemblyHandle() const { return AssemblyGCHandle; }

private:

	void OnTypeReflectionDataChanged(TSharedPtr<FCSManagedTypeDefinition> ManagedTypeDefinition);

	// Map of all Unreal types defined in this assembly, keyed by their field name (namespace + name).
	TMap<FCSFieldName, TSharedPtr<FCSManagedTypeDefinition>> DefinedManagedTypes;

	// List of managed types that require rebuilding due to changes or dependencies.
	TArray<TSharedPtr<FCSManagedTypeDefinition>> PendingRebuildTypes;
	
	// All handles allocated by this assembly. Handles to types, methods, objects.
	TArray<TSharedPtr<FGCHandle>> AllocatedGCHandles;

	// Handles to all allocated UTypes (UClass/UStruct, etc) that are defined in this assembly.
	TMap<FCSFieldName, TSharedPtr<FGCHandle>> ManagedTypeGCHandles;

	// Handle to the Assembly object in C#.
	TSharedPtr<FGCHandle> AssemblyGCHandle;

	// Full path to the assembly file.
	FString AssemblyFilePath;

	// Assembly file name without the path.
	FName AssemblyName;
	
	bool bIsLoading = false;
	
#if WITH_EDITORONLY_DATA
	UPROPERTY(Transient)
	TArray<TObjectPtr<UCSManagedAssembly>> DependentAssemblies;
#endif
};