#pragma once

#include "CSManagedGCHandle.h"
#include "MetaData/CSTypeReferenceMetaData.h"

class UCSGeneratedTypeBuilder;
class UCSAssembly;
struct FGCHandle;

DECLARE_MULTICAST_DELEGATE_OneParam(FOnStructureChanged, TSharedPtr<struct FCSManagedTypeInfo>);

struct UNREALSHARPCORE_API FCSManagedTypeInfoDelegates
{
	static FDelegateHandle AddOnStructureChangedDelegate(const FOnStructureChanged::FDelegate& Delegate)
	{
		return OnStructureChangedDelegate.Add(Delegate);
	}

	static void RemoveOnStructureChangedDelegate(FDelegateHandle DelegateHandle)
	{
		OnStructureChangedDelegate.Remove(DelegateHandle);
	}

private:
	friend struct FCSManagedTypeInfo;
	static FOnStructureChanged OnStructureChangedDelegate;
};

struct UNREALSHARPCORE_API FCSManagedTypeInfo final : TSharedFromThis<FCSManagedTypeInfo>
{
	~FCSManagedTypeInfo() = default;
	FCSManagedTypeInfo() = default;

	static TSharedPtr<FCSManagedTypeInfo> CreateManaged(TSharedPtr<FCSTypeReferenceMetaData> MetaData, UCSAssembly* InOwningAssembly, UCSGeneratedTypeBuilder* Builder);
	static TSharedPtr<FCSManagedTypeInfo> CreateNative(UField* InField, UCSAssembly* InOwningAssembly);

#if WITH_EDITOR
	TSharedPtr<FGCHandle> GetTypeHandle();
#else
	TSharedPtr<FGCHandle> GetTypeHandle() const { return TypeHandle; }
#endif

	UField* GetOrBuildField();
	UField* GetField() const { return Field.Get(); }

	template<typename TMetaData = FCSTypeReferenceMetaData>
	TSharedPtr<TMetaData> GetMetaData() const
	{
		static_assert(TIsDerivedFrom<TMetaData, FCSTypeReferenceMetaData>::Value, "TMetaData must be a FCSTypeReferenceMetaData-derived type.");
		return StaticCastSharedPtr<TMetaData>(MetaData);
	}

	UCSAssembly* GetOwningAssembly() const { return OwningAssembly; }

	void SetMetaData(const TSharedPtr<FCSTypeReferenceMetaData>& InMetaData)
	{
		if (MetaData)
		{
			checkf(MetaData->FieldName == InMetaData->FieldName, TEXT("Cannot change MetaData to a different type's metadata."));
		}
		
		MetaData = InMetaData;
	}
	void SetTypeHandle(uint8* TypeHandlePtr);
	
	void MarkAsStructurallyModified();

	bool HasStructurallyChanged() const { return bHasChangedStructure; }
	
private:

	// The actual UClass, UStruct, UEnum et.c. that this managed type info represents.
	TStrongObjectPtr<UField> Field;

	// The CDO that's responsible for creating/updating the managed type. UCSGeneratedClassBuilder, UCSGeneratedStructBuilder et.c.
	UCSGeneratedTypeBuilder* Builder;

	// The managed assembly that owns this type. Assembly is always in memory so a raw pointer is fine.
	UCSAssembly* OwningAssembly;
	
	// The metadata for this type to generate reflection data from (UProperties, UFunctions, metadata et.c.).
	TSharedPtr<FCSTypeReferenceMetaData> MetaData;

	// Current state of the structure of this type. This changes when new UProperties/UFunctions/metadata are added or removed from the type.
	bool bHasChangedStructure = false;

	// Handle to the managed type in the C# assembly.
	TSharedPtr<FGCHandle> TypeHandle;
};
