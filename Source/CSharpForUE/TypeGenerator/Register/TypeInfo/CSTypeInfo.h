#pragma once

#include "CSharpForUE/CSManager.h"

template<typename TMetaData, typename TField, typename TTypeBuilder>
struct CSHARPFORUE_API TCSharpTypeInfo
{
public:
	virtual ~TCSharpTypeInfo() = default;

	TCSharpTypeInfo(const TSharedPtr<FJsonValue>& MetaData) : TypeMetaData(nullptr), TypeHandle(nullptr), Field(nullptr)
	{
		TypeMetaData = MakeShared<TMetaData>();
		TypeMetaData->SerializeFromJson(MetaData->AsObject());
		TypeHandle = FCSManager::Get().GetTypeHandle(*TypeMetaData);
	}

	TCSharpTypeInfo() : TypeHandle(nullptr), Field(nullptr) {}
	
	// The meta data for this type (properties, functions et.c.)
	TSharedPtr<TMetaData> TypeMetaData;

	// Pointer to the TypeHandle in CSharp
	uint8* TypeHandle;

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
