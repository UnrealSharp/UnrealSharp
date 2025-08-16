#pragma once
#include "CSManagedGCHandle.h"

class UCSAssembly;
struct FGCHandle;
struct FCSTypeReferenceMetaData;

enum ECSStructureState : uint8
{
	UpToDate,
	HasChangedStructure,
};

struct UNREALSHARPCORE_API FCSManagedTypeInfo : TSharedFromThis<FCSManagedTypeInfo>
{
	virtual ~FCSManagedTypeInfo() = default;
	
	FCSManagedTypeInfo(const TSharedPtr<FCSTypeReferenceMetaData>& MetaData, UCSAssembly* InOwningAssembly, UClass* InTypeClass);
	FCSManagedTypeInfo(UField* NativeField, UCSAssembly* InOwningAssembly);
	
	TSharedPtr<FGCHandle> GetManagedTypeHandle()
	{
#if WITH_EDITOR
		if (!ManagedTypeHandle.IsValid() || ManagedTypeHandle->IsNull())
		{
			// Lazy load the type handle in editor if it is not already set.
			return FindTypeHandle();
		}
#endif
		return ManagedTypeHandle;
	}
	
	template<typename TField = UField>
	TField* GetFieldChecked() const
	{
		return CastChecked<TField>(Field.Get());
	}

	template<typename TMetaData = FCSTypeReferenceMetaData>
	TSharedPtr<TMetaData> GetTypeMetaData() const
	{
		static_assert(TIsDerivedFrom<TMetaData, FCSTypeReferenceMetaData>::Value, "TMetaData must be a FCSTypeReferenceMetaData-derived type.");
		return StaticCastSharedPtr<TMetaData>(TypeMetaData);
	}

	UCSAssembly* GetOwningAssembly() const { return OwningAssembly.Get(); }

	void SetStructureState(ECSStructureState NewState) { StructureState = NewState; }
	ECSStructureState GetStructureState() const { return StructureState; }

	void SetTypeMetaData(const TSharedPtr<FCSTypeReferenceMetaData>& InTypeMetaData) { TypeMetaData = InTypeMetaData; }

	UClass* GetFieldClass() const { return FieldClass.Get(); }

	bool IsNativeType() const { return !TypeMetaData.IsValid(); }

protected:

	friend UCSAssembly;

	// FCSManagedTypeInfo interface
	virtual UField* StartBuildingManagedType();
	// End

	// Pointer to the native field of this type.
	TStrongObjectPtr<UField> Field;
	TStrongObjectPtr<UClass> FieldClass;

	// The managed assembly that owns this type.
	TWeakObjectPtr<UCSAssembly> OwningAssembly;
	
	// The metadata for this type (properties, functions et.c.)
	TSharedPtr<FCSTypeReferenceMetaData> TypeMetaData;

	// Current state of the structure of this type. This changes when new UProperties/UFunctions/metadata are added or removed.
	ECSStructureState StructureState = HasChangedStructure;

	// Handle to the managed type in the C# assembly.
	TSharedPtr<FGCHandle> ManagedTypeHandle;

private:
	TSharedPtr<FGCHandle> FindTypeHandle() const;
};
