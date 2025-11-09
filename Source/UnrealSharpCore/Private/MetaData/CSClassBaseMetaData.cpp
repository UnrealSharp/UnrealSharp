#include "MetaData/CSClassBaseMetaData.h"

bool FCSClassBaseMetaData::Serialize(TSharedPtr<FJsonObject> JsonObject)
{
	START_JSON_SERIALIZE
		
	CALL_SERIALIZE(FCSStructMetaData::Serialize(JsonObject));
		
	JSON_PARSE_OBJECT(ParentClass, IS_REQUIRED);
	JSON_PARSE_OBJECT_ARRAY(Functions, IS_OPTIONAL);
	JSON_READ_ENUM(ClassFlags, IS_REQUIRED);
	JSON_READ_STRING(ConfigName, IS_OPTIONAL);
		
	END_JSON_SERIALIZE
}
