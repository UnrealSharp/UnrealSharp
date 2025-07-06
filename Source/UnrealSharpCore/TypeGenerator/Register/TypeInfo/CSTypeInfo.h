#pragma once

struct FCSAssembly;

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

template<typename TMetaData, typename TField, typename TTypeBuilder>
struct UNREALSHARPCORE_API TCSTypeInfo
{
	virtual ~TCSTypeInfo() = default;

	TCSTypeInfo(const TSharedPtr<FJsonValue>& MetaData, const TSharedPtr<FCSAssembly>& InOwningAssembly) : Field(nullptr), OwningAssembly(InOwningAssembly)
	{
		TypeMetaData = MakeShared<TMetaData>();
		TypeMetaData->SerializeFromJson(MetaData->AsObject());
	}

	TCSTypeInfo() : Field(nullptr)
	{
		
	}
	
	// The metadata for this type (properties, functions et.c.)
	TSharedPtr<TMetaData> TypeMetaData;

	// Pointer to the field of this type
	TField* Field;

	// Current state of the type
	ETypeState State = ETypeState::NeedRebuild;

	// Owning assembly
	TSharedPtr<FCSAssembly> OwningAssembly;

	virtual TField* InitializeBuilder()
	{
		if (Field && (State == UpToDate || State == CurrentlyBuilding))
        {
			// No need to rebuild or update
            return Field;
        }
		
		// Builder for this type
		TTypeBuilder TypeBuilder(TypeMetaData, OwningAssembly);
		Field = TypeBuilder.CreateType();
		
		if (State == NeedRebuild)
		{
			State = CurrentlyBuilding;
			TypeBuilder.RebuildType();
        }
#if WITH_EDITOR
        else if (State == NeedUpdate)
        {
        	State = CurrentlyBuilding;
            TypeBuilder.UpdateType();
		}
#endif
	
		State = UpToDate;
		return Field;
	}
};
