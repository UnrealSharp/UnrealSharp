#pragma once

struct FCSAssembly;

enum ETypeState
{
	None,
	NeedRebuild,
	NeedUpdate,
};

template<typename TMetaData, typename TField, typename TTypeBuilder>
struct UNREALSHARPCORE_API TCSharpTypeInfo
{
	virtual ~TCSharpTypeInfo() = default;

	TCSharpTypeInfo(const TSharedPtr<FJsonValue>& MetaData, const TSharedPtr<FCSAssembly>& InOwningAssembly) : Field(nullptr), OwningAssembly(InOwningAssembly)
	{
		TypeMetaData = MakeShared<TMetaData>();
		TypeMetaData->SerializeFromJson(MetaData->AsObject());
	}

	TCSharpTypeInfo() : Field(nullptr)
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
		// Builder for this type
		TTypeBuilder TypeBuilder(TypeMetaData);
		Field = TypeBuilder.CreateType();
		
		if (State == NeedRebuild)
		{
			TypeBuilder.RebuildType();
        }
        else if (State == NeedUpdate)
        {
            TypeBuilder.UpdateType();
		}
	
		State = None;
		return Field;
	}
};
