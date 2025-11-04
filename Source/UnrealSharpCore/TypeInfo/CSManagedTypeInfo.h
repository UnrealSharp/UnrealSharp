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
	FCSManagedTypeInfo(UField* NativeField, UCSAssembly* InOwningAssembly);
	FCSManagedTypeInfo(TSharedPtr<FCSTypeReferenceMetaData> MetaData, UCSAssembly* InOwningAssembly, UCSGeneratedTypeBuilder* Builder);

	static TSharedPtr<FCSManagedTypeInfo> Create(TSharedPtr<FCSTypeReferenceMetaData> MetaData, UCSAssembly* InOwningAssembly, UCSGeneratedTypeBuilder* Builder);

	TSharedPtr<FGCHandle> GetManagedTypeHandle() { return ManagedTypeHandle; }

	UField* GetOrBuildField();
	UField* GetField() const { return Field.Get(); }

	template<typename TMetaData = FCSTypeReferenceMetaData>
	TSharedPtr<TMetaData> GetTypeMetaData() const
	{
		static_assert(TIsDerivedFrom<TMetaData, FCSTypeReferenceMetaData>::Value, "TMetaData must be a FCSTypeReferenceMetaData-derived type.");
		return StaticCastSharedPtr<TMetaData>(TypeMetaData);
	}

	UCSAssembly* GetOwningAssembly() const { return OwningAssembly; }

	void SetTypeMetaData(const TSharedPtr<FCSTypeReferenceMetaData>& InTypeMetaData) { TypeMetaData = InTypeMetaData; }
	void SetTypeHandle(uint8* ManagedTypeHandlePtr);
	
	void MarkAsStructurallyModified();
	bool HasChangedStructure() const { return bHasChangedStructure; }
	
private:
	friend UCSAssembly;

	// The actual UClass, UStruct, UEnum et.c. that this managed type info represents.
	TStrongObjectPtr<UField> Field;

	// The CDO that's responsible for creating/updating the managed type. UCSGeneratedClassBuilder, UCSGeneratedStructBuilder et.c.
	UCSGeneratedTypeBuilder* Builder;

	// The managed assembly that owns this type. Assembly is always in memory so a raw pointer is fine.
	UCSAssembly* OwningAssembly;
	
	// The metadata for this type to generate reflection data from (UProperties, UFunctions, metadata et.c.).
	TSharedPtr<FCSTypeReferenceMetaData> TypeMetaData;

	// Current state of the structure of this type. This changes when new UProperties/UFunctions/metadata are added or removed from the type.
	bool bHasChangedStructure = false;

	// Handle to the managed type in the C# assembly.
	TSharedPtr<FGCHandle> ManagedTypeHandle;
};
