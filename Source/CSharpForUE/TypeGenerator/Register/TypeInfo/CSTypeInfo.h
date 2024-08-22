#pragma once

template<typename TMetaData, typename TField, typename TTypeBuilder>
struct CSHARPFORUE_API TCSharpTypeInfo
{
	virtual ~TCSharpTypeInfo() = default;

	TCSharpTypeInfo(const TSharedPtr<FJsonValue>& MetaData) : TypeMetaData(nullptr), Field(nullptr)
	{
		TypeMetaData = MakeShared<TMetaData>();
		TypeMetaData->SerializeFromJson(MetaData->AsObject());
	}

	TCSharpTypeInfo() : Field(nullptr) {}
	
	// The meta data for this type (properties, functions et.c.)
	TSharedPtr<TMetaData> TypeMetaData;

	// Pointer to the field of this type
	TField* Field;

	virtual TField* InitializeBuilder()
	{
		if (Field)
		{
			return Field;
		}
		
		TTypeBuilder TypeBuilder = TTypeBuilder(TypeMetaData);
		Field = TypeBuilder.CreateType();
		TypeBuilder.StartBuildingType();
		return Field;
	}
};
