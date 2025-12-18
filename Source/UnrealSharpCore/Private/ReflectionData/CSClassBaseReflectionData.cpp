#include "ReflectionData/CSClassBaseReflectionData.h"

bool FCSClassBaseReflectionData::Serialize(TSharedPtr<FJsonObject> JsonObject)
{
	START_JSON_SERIALIZE
		
	CALL_SERIALIZE(FCSStructReflectionData::Serialize(JsonObject));
	
	JSON_PARSE_OBJECT_ARRAY(Functions, IS_OPTIONAL);
	JSON_READ_ENUM(ClassFlags, IS_OPTIONAL);
	JSON_READ_STRING(Config, IS_OPTIONAL);
		
	END_JSON_SERIALIZE
}
