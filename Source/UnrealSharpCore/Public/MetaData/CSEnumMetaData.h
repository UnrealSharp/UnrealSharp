#pragma once

#include "CSTypeReferenceMetaData.h"

struct FCSEnumMetaData : FCSTypeReferenceMetaData
{
	TArray<FString> Items;

	virtual bool Serialize(TSharedPtr<FJsonObject> JsonObject) override
	{
		START_JSON_SERIALIZE
		
		CALL_SERIALIZE(FCSTypeReferenceMetaData::Serialize(JsonObject));
		JSON_READ_STRING_ARRAY(Items, IS_OPTIONAL);
		
		END_JSON_SERIALIZE
	}
};
