#pragma once

#include "CSManagedGCHandle.h"
#include "MetaData/CSTypeReferenceMetaData.h"

class UCSGeneratedTypeBuilder;
class UCSAssembly;
struct FGCHandle;

enum ECSStructureState : uint8
{
	UpToDate,
	HasChangedStructure,
};

struct UNREALSHARPCORE_API FCSManagedTypeInfo : TSharedFromThis<FCSManagedTypeInfo>
{
	virtual ~FCSManagedTypeInfo() = default;
	
	FCSManagedTypeInfo(TSharedPtr<FCSTypeReferenceMetaData> MetaData, UCSAssembly* InOwningAssembly);
	FCSManagedTypeInfo(UField* NativeField, UCSAssembly* InOwningAssembly);
	
	TSharedPtr<FGCHandle> GetManagedTypeHandle() { return ManagedTypeHandle;}
	
	template<typename TField = UField>
	TField* GetFieldChecked() const
	{
		return CastChecked<TField>(Field);
	}

	template<typename TMetaData = FCSTypeReferenceMetaData>
	TSharedPtr<TMetaData> GetTypeMetaData() const
	{
		static_assert(TIsDerivedFrom<TMetaData, FCSTypeReferenceMetaData>::Value, "TMetaData must be a FCSTypeReferenceMetaData-derived type.");
		return StaticCastSharedPtr<TMetaData>(TypeMetaData);
	}

	UCSAssembly* GetOwningAssembly() const { return OwningAssembly.Get(); }
	
	ECSStructureState GetStructureState() const { return StructureState; }

	void SetTypeMetaData(const TSharedPtr<FCSTypeReferenceMetaData>& InTypeMetaData) { TypeMetaData = InTypeMetaData; }

	UClass* GetFieldClass() const { return TypeMetaData->FieldClass; }

	bool IsNativeType() const { return !TypeMetaData.IsValid(); }

#if WITH_EDITOR
	uint32 GetLastModifiedTime() const { return LastModifiedTime; }
	void SetLastModifiedTime(uint32 InLastModifiedTime) { LastModifiedTime = InLastModifiedTime; }
#endif

	void SetTypeHandle(uint8* ManagedTypeHandlePtr);

	// FCSManagedTypeInfo interface
	virtual void OnStructureChanged();
protected:
	virtual UField* StartBuildingType();
	// End

	friend UCSAssembly;

	// Pointer to the native field of this type. It's fine to not have UPROPERTY here, they're always in the root set.
	UField* Field;
	const UCSGeneratedTypeBuilder* CachedTypeBuilder;

	// The managed assembly that owns this type.
	TWeakObjectPtr<UCSAssembly> OwningAssembly;
	
	// The metadata for this type (properties, functions et.c.)
	TSharedPtr<FCSTypeReferenceMetaData> TypeMetaData;

	// Current state of the structure of this type. This changes when new UProperties/UFunctions/metadata are added or removed.
	ECSStructureState StructureState = HasChangedStructure;

	// Handle to the managed type in the C# assembly.
	TSharedPtr<FGCHandle> ManagedTypeHandle;

#if WITH_EDITOR
	uint32 LastModifiedTime;
#endif
};
