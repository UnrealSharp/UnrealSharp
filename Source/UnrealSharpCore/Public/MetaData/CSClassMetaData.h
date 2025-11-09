#pragma once

#include "CSClassBaseMetaData.h"
#include "CSTypeReferenceMetaData.h"

struct FCSClassMetaData : FCSClassBaseMetaData
{
	TArray<FName> Overrides;
	TArray<FCSTypeReferenceMetaData> Interfaces;

	// FCSMetaDataBase interface
	virtual bool Serialize(TSharedPtr<FJsonObject> JsonObject) override
	{
		START_JSON_SERIALIZE
		
		CALL_SERIALIZE(FCSClassBaseMetaData::Serialize(JsonObject));
		JSON_READ_STRING_ARRAY(Overrides, IS_OPTIONAL);
		JSON_PARSE_OBJECT_ARRAY(Interfaces, IS_OPTIONAL);
		
		END_JSON_SERIALIZE
	}
	// End of FCSMetaDataBase interface
};
