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

UCLASS()
class UCSManagedAssembly : public UObject
{
	GENERATED_BODY()
public:
	void InitializeManagedAssembly(FStringView InAssemblyPath, bool bIsCollectible = false);

	UNREALSHARPCORE_API bool LoadManagedAssembly();
	UNREALSHARPCORE_API bool UnloadManagedAssembly();

	UNREALSHARPCORE_API bool IsAssemblyLoading() const { return bIsLoading; }
	UNREALSHARPCORE_API bool IsAssemblyLoaded() const { return AssemblyGCHandle.IsValid() && !AssemblyGCHandle->IsNull(); }
	
	UNREALSHARPCORE_API FName GetAssemblyName() const { return AssemblyName; }
	UNREALSHARPCORE_API const FString& GetAssemblyFilePath() const { return AssemblyFilePath; }
	UNREALSHARPCORE_API FString GetAssemblyFileName() const { return FPaths::GetCleanFilename(AssemblyFilePath); }
	
	UNREALSHARPCORE_API const TMap<FCSFieldName, TSharedPtr<FCSManagedTypeDefinition>>& GetDefinedManagedTypes() const { return DefinedManagedTypes; }
	UNREALSHARPCORE_API bool IsCollectible() const { return bIsCollectible; }
	
#if WITH_EDITOR
	UNREALSHARPCORE_API void AddDependentAssembly(UCSManagedAssembly* DependencyAssembly) { DependentAssemblies.Add(DependencyAssembly); }
	UNREALSHARPCORE_API const TArray<UCSManagedAssembly*>& GetDependentAssemblies() const { return DependentAssemblies; }
#endif

	TSharedPtr<FGCHandle> FindTypeHandle(const FCSFieldName& FieldName);
	TSharedPtr<FGCHandle> AddTypeHandle(const FCSFieldName& FieldName, uint8* TypeHandle);
	TSharedPtr<FGCHandle> GetManagedMethod(const TSharedPtr<FGCHandle>& TypeHandle, const FString& MethodName);

	TSharedPtr<FCSManagedTypeDefinition> FindOrAddManagedTypeDefinition(UClass* Field);
	TSharedPtr<FCSManagedTypeDefinition> FindOrAddManagedTypeDefinition(const FCSFieldName& ClassName);
	UNREALSHARPCORE_API TSharedPtr<FCSManagedTypeDefinition> FindManagedTypeDefinition(const FCSFieldName& FieldName) const;

	template<typename T = UField>
	T* ResolveUField(const FCSFieldName& FieldName) const
	{
		static_assert(TIsDerivedFrom<T, UField>::Value, "T must be a UField-derived type.");
		TRACE_CPUPROFILER_EVENT_SCOPE(UCSAssembly::ResolveUField);

		if (TSharedPtr<FCSManagedTypeDefinition> ManagedTypeDefinition = FindManagedTypeDefinition(FieldName))
		{
			return Cast<T>(ManagedTypeDefinition->CompileAndGetDefinitionField());
		}
		
		return FCSUtilities::FindField<T>(FieldName);
	}

	TSharedPtr<FCSManagedTypeDefinition> RegisterManagedType(TCHAR* InFieldName, const TCHAR* InNamespace, ECSFieldType FieldType, uint8* TypeGCHandle, TCHAR* NewJsonReflectionData);

	TSharedPtr<FGCHandle> CreateManagedObjectFromNative(const UObject* Object);
	TSharedPtr<FGCHandle> CreateManagedObjectFromNative(const UObject* Object, const TSharedPtr<FGCHandle>& TypeGCHandle);
	TSharedPtr<FGCHandle> GetOrCreateManagedInterface(UObject* Object, UClass* InterfaceClass);

	TSharedPtr<const FGCHandle> GetManagedAssemblyHandle() const { return AssemblyGCHandle; }

private:
	void OnTypeReflectionDataChanged(TSharedPtr<FCSManagedTypeDefinition> ManagedTypeDefinition);

	TMap<FCSFieldName, TSharedPtr<FCSManagedTypeDefinition>> DefinedManagedTypes;
	TArray<TSharedPtr<FCSManagedTypeDefinition>> PendingRebuildTypes;
	TArray<TSharedPtr<FGCHandle>> AllocatedGCHandles;
	TMap<FCSFieldName, TSharedPtr<FGCHandle>> ManagedTypeGCHandles;
	TSharedPtr<FGCHandle> AssemblyGCHandle;

	FString AssemblyFilePath;
	FName AssemblyName;
	
	bool bIsLoading = false;
	bool bIsCollectible = false;
	
#if WITH_EDITORONLY_DATA
	UPROPERTY(Transient)
	TArray<TObjectPtr<UCSManagedAssembly>> DependentAssemblies;
#endif
};