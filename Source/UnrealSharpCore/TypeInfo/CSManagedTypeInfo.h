#pragma once

#include "CSManagedGCHandle.h"
#include "MetaData/CSTypeReferenceMetaData.h"

class UCSGeneratedTypeBuilder;
class UCSAssembly;
struct FGCHandle;

struct UNREALSHARPCORE_API FCSManagedTypeInfo final : TSharedFromThis<FCSManagedTypeInfo>
{
	DECLARE_MULTICAST_DELEGATE_OneParam(FOnStructureChanged, TSharedPtr<FCSManagedTypeInfo> /*ManagedTypeInfo*/);
	
	~FCSManagedTypeInfo() = default;
	
	FCSManagedTypeInfo(UField* NativeField, UCSAssembly* InOwningAssembly);
	FCSManagedTypeInfo(TSharedPtr<FCSTypeReferenceMetaData> MetaData, UCSAssembly* InOwningAssembly, UCSGeneratedTypeBuilder* Builder)
	: Field(nullptr), Builder(Builder), OwningAssembly(InOwningAssembly), TypeMetaData(MetaData)
	{
		
	}

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

#if WITH_EDITOR
	uint32 GetLastModifiedTime() const { return LastModifiedTime; }
	void SetLastModifiedTime(uint32 InLastModifiedTime) { LastModifiedTime = InLastModifiedTime; }
#endif

	void SetTypeMetaData(const TSharedPtr<FCSTypeReferenceMetaData>& InTypeMetaData) { TypeMetaData = InTypeMetaData; }
	void SetTypeHandle(uint8* ManagedTypeHandlePtr);
	
	void MarkAsStructurallyModified();

	FDelegateHandle RegisterOnStructureChanged(const FOnStructureChanged::FDelegate& Delegate)
	{
		return OnStructureChanged.Add(Delegate);
	}

	void UnregisterOnStructureChanged(FDelegateHandle DelegateHandle)
	{
		OnStructureChanged.Remove(DelegateHandle);
	}
	
private:
	void SetField(UField* InField) { Field = TStrongObjectPtr(InField); }
	
	friend UCSAssembly;
	
	TStrongObjectPtr<UField> Field;
	UCSGeneratedTypeBuilder* Builder;

	// The managed assembly that owns this type.
	UCSAssembly* OwningAssembly;
	
	// The metadata for this type (properties, functions et.c.)
	TSharedPtr<FCSTypeReferenceMetaData> TypeMetaData;

	// Current state of the structure of this type. This changes when new UProperties/UFunctions/metadata are added or removed.
	bool bHasChangedStructure = false;

	// Handle to the managed type in the C# assembly.
	TSharedPtr<FGCHandle> ManagedTypeHandle;

	FOnStructureChanged OnStructureChanged;

#if WITH_EDITOR
	uint32 LastModifiedTime = 0;
#endif
};
