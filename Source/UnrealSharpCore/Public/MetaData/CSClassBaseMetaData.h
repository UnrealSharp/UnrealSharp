#pragma once

#include "CSFunctionMetaData.h"
#include "CSStructMetaData.h"

struct FCSClassBaseMetaData : FCSStructMetaData
{
	// FCSMetaDataBase interface
	virtual bool Serialize(TSharedPtr<FJsonObject> JsonObject) override
	{
		START_JSON_SERIALIZE
		
		CALL_SERIALIZE(FCSStructMetaData::Serialize(JsonObject));
		
		JSON_PARSE_OBJECT(ParentClass, IS_REQUIRED);
		JSON_PARSE_OBJECT_ARRAY(Functions, IS_OPTIONAL);
		JSON_READ_ENUM(ClassFlags, IS_REQUIRED);
		JSON_READ_STRING(ConfigName, IS_OPTIONAL);
		
		END_JSON_SERIALIZE
	}
	// End of FCSMetaDataBase interface
	
	FCSTypeReferenceMetaData ParentClass;
	TArray<FCSFunctionMetaData> Functions;
	EClassFlags ClassFlags;
	FName ConfigName;
};
