#pragma once

class UCSAssembly;
struct FGCHandle;
struct FCSTypeReferenceMetaData;

enum ETypeState : uint8
{
	// The type is up to date. No need to rebuild or update.
	UpToDate,

	// The type needs to be rebuilt. The structure has changed.
	NeedRebuild,

	// The type just needs to be updated. New method ptr et.c.
	NeedUpdate,

	// This type is currently being built. Used to prevent circular dependencies.
	CurrentlyBuilding,
};

struct UNREALSHARPCORE_API FCSManagedTypeInfo : TSharedFromThis<FCSManagedTypeInfo>
{
	virtual ~FCSManagedTypeInfo() = default;
	
	FCSManagedTypeInfo(const TSharedPtr<FCSTypeReferenceMetaData>& MetaData, UCSAssembly* InOwningAssembly, UClass* InClass);
	FCSManagedTypeInfo(UField* InField, UCSAssembly* InOwningAssembly, const TSharedPtr<FGCHandle>& TypeHandle);

#if WITH_EDITOR
	TSharedPtr<FGCHandle> GetManagedTypeHandle();
#else
	TSharedPtr<FGCHandle> GetManagedTypeHandle()
	{
		return ManagedTypeHandle;
	}
#endif
	
	template<typename TField = UField>
	TField* GetField() const
	{
		static_assert(TIsDerivedFrom<TField, UField>::Value, "T must be a UField-derived type.");
		return static_cast<TField*>(Field);
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

	UClass* GetFieldClass() const { return FieldClass; }
	
	// FCSManagedTypeInfo interface
	virtual UField* InitializeBuilder();
	// End

protected:

	friend UCSAssembly;

	// Pointer to the native field of this type.
	UField* Field;
	UClass* FieldClass;

	// The managed assembly that has this type
	TWeakObjectPtr<UCSAssembly> OwningAssembly;
	
	// The metadata for this type (properties, functions et.c.)
	TSharedPtr<FCSTypeReferenceMetaData> TypeMetaData;

	// Current state of the type
	ETypeState State = ETypeState::NeedRebuild;

	// Handle to the managed type in the C# assembly.
	TSharedPtr<FGCHandle> ManagedTypeHandle;
};
