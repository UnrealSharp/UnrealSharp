#pragma once

#include "CSTypeReferenceMetaData.h"
#include "CSUnrealType.h"

struct FCSFieldTypePropertyMetaData : FCSUnrealType
{
	FCSTypeReferenceMetaData InnerType;

	// FCSMetaDataBase interface
	virtual bool Serialize(TSharedPtr<FJsonObject> JsonObject) override
	{
		START_JSON_SERIALIZE
		
		CALL_SERIALIZE(FCSUnrealType::Serialize(JsonObject));
		JSON_PARSE_OBJECT(InnerType, IS_REQUIRED);

		END_JSON_SERIALIZE
	}
	// End of FCSMetaDataBase interface
};
