#pragma once

#include "CSPropertyMetaData.h"
#include "CSTypeReferenceMetaData.h"

struct FCSStructMetaData : FCSTypeReferenceMetaData
{
	TArray<FCSPropertyMetaData> Properties;

	virtual bool Serialize(TSharedPtr<FJsonObject> JsonObject) override
	{
		START_JSON_SERIALIZE
		
		CALL_SERIALIZE(FCSTypeReferenceMetaData::Serialize(JsonObject));
		JSON_PARSE_OBJECT_ARRAY(Properties, IS_OPTIONAL);
		
		END_JSON_SERIALIZE
	}
};
