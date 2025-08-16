#pragma once
#include "CSManagedGCHandle.h"

class UCSAssembly;
struct FGCHandle;
struct FCSTypeReferenceMetaData;

enum ETypeState : uint8
{
	UpToDate,
	HasChangedStructure,
	CurrentlyBuilding,
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
	TField* GetField() const
	{
		static_assert(TIsDerivedFrom<TField, UField>::Value, "T must be a UField-derived type.");
		return static_cast<TField*>(Field.Get());
	}

	template<typename TMetaData = FCSTypeReferenceMetaData>
	TSharedPtr<TMetaData> GetTypeMetaData() const
	{
		static_assert(TIsDerivedFrom<TMetaData, FCSTypeReferenceMetaData>::Value, "TMetaData must be a FCSTypeReferenceMetaData-derived type.");
		return StaticCastSharedPtr<TMetaData>(TypeMetaData);
	}

	UCSAssembly* GetOwningAssembly() const { return OwningAssembly.Get(); }

	void SetState(ETypeState NewState) { State = NewState; }
	ETypeState GetState() const { return State; }

	void SetTypeMetaData(const TSharedPtr<FCSTypeReferenceMetaData>& InTypeMetaData) { TypeMetaData = InTypeMetaData; }

	UClass* GetFieldClass() const { return FieldClass.Get(); }

	bool IsNativeType() const { return !TypeMetaData.IsValid(); }
	
	// FCSManagedTypeInfo interface
	virtual UField* StartBuildingType();
	// End

protected:

	friend UCSAssembly;

	// Pointer to the native field of this type.
	TStrongObjectPtr<UField> Field;
	TStrongObjectPtr<UClass> FieldClass;

	// The managed assembly that owns this type.
	TWeakObjectPtr<UCSAssembly> OwningAssembly;
	
	// The metadata for this type (properties, functions et.c.)
	TSharedPtr<FCSTypeReferenceMetaData> TypeMetaData;

	// Current state of the type, mainly used for hot reloading
	ETypeState State = ETypeState::HasChangedStructure;

	// Handle to the managed type in the C# assembly.
	TSharedPtr<FGCHandle> ManagedTypeHandle;

private:
	TSharedPtr<FGCHandle> FindTypeHandle() const;
};
