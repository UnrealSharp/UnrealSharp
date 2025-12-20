#pragma once

#include "CSManagedGCHandle.h"
#include "ReflectionData/CSTypeReferenceReflectionData.h"

class UCSManagedTypeCompiler;
class UCSManagedAssembly;
struct FGCHandle;

DECLARE_MULTICAST_DELEGATE_OneParam(FOnManagedTypeStructureChanged, TSharedPtr<struct FCSManagedTypeDefinition>);

struct UNREALSHARPCORE_API FCSManagedTypeDefinitionEvents
{
	static FDelegateHandle AddOnReflectionDataChangedDelegate(const FOnManagedTypeStructureChanged::FDelegate& Delegate)
	{
		return OnReflectionDataChanged.Add(Delegate);
	}

	static void RemoveOnReflectionDataChangedDelegate(FDelegateHandle DelegateHandle)
	{
		OnReflectionDataChanged.Remove(DelegateHandle);
	}

private:
	friend struct FCSManagedTypeDefinition;
	static FOnManagedTypeStructureChanged OnReflectionDataChanged;
};

struct UNREALSHARPCORE_API FCSManagedTypeDefinition final : TSharedFromThis<FCSManagedTypeDefinition>
{
	~FCSManagedTypeDefinition() = default;
	FCSManagedTypeDefinition() = default;

	static TSharedPtr<FCSManagedTypeDefinition> CreateFromReflectionData(const TSharedPtr<FCSTypeReferenceReflectionData>& InReflectionData, UCSManagedAssembly* InOwningAssembly, UCSManagedTypeCompiler* InCompiler);
	static TSharedPtr<FCSManagedTypeDefinition> CreateFromNativeField(UField* InField, UCSManagedAssembly* InOwningAssembly);

#if WITH_EDITOR
	TSharedPtr<FGCHandle> GetTypeGCHandle();
#else
	TSharedPtr<FGCHandle> GetTypeGCHandle() const { return TypeGCHandle; }
#endif

	UField* CompileAndGetDefinitionField();
	UField* GetDefinitionField() const { return DefinitionField.Get(); }

	template<typename TReflectionData = FCSTypeReferenceReflectionData>
	TSharedPtr<TReflectionData> GetReflectionData() const
	{
		static_assert(TIsDerivedFrom<TReflectionData, FCSTypeReferenceReflectionData>::Value, "TReflectionData must be a FCSTypeReferenceReflectionData-derived type.");
		return StaticCastSharedPtr<TReflectionData>(ReflectionData);
	}

	UCSManagedAssembly* GetOwningAssembly() const { return OwningAssembly; }

	void SetReflectionData(const TSharedPtr<FCSTypeReferenceReflectionData>& InReflectionData)
	{
		ReflectionData = InReflectionData;
		FCSManagedTypeDefinitionEvents::OnReflectionDataChanged.Broadcast(SharedThis(this));
	}
	
	void SetTypeGCHandle(uint8* GCHandlePtr);
	
	void MarkStructurallyDirty();
	bool HasStructuralChanges() const { return bHasChangedStructure; }
	
private:

	// The Unreal reflection type generated for this managed definition.
	// This may be a UClass, UStruct, UEnum, UInterface, UFunction (Delegate), et.c depending on ReflectionData.
	TStrongObjectPtr<UField> DefinitionField;

	// Compiler responsible for creating and updating the native Unreal type from the managed reflection data.
	UCSManagedTypeCompiler* Compiler;

	// The managed assembly that owns this type. Always in memory, so a raw pointer is safe and intentional.
	UCSManagedAssembly* OwningAssembly;
	
	// The data describing this type (properties, functions, interfaces...)
	// Used to compile the native Unreal type in the UCSManagedTypeCompiler.
	TSharedPtr<FCSTypeReferenceReflectionData> ReflectionData;

	// Indicates whether the structure of this type has changed since last compilation.
	// Set when properties, functions, or metadata are added/removed, triggering regeneration.
	bool bHasChangedStructure = false;

	// Handle to the underlying managed (C#) type
	TSharedPtr<FGCHandle> TypeGCHandle;
};
