#include "ReflectionData/CSClassReflectionData.h"

bool FCSClassReflectionData::Serialize(TSharedPtr<FJsonObject> JsonObject)
{
	START_JSON_SERIALIZE
		
	CALL_SERIALIZE(FCSClassBaseReflectionData::Serialize(JsonObject));
	JSON_PARSE_OBJECT(ParentClass, IS_REQUIRED);
	JSON_READ_STRING_ARRAY(Overrides, IS_OPTIONAL);
	JSON_PARSE_OBJECT_ARRAY(Interfaces, IS_OPTIONAL);
	JSON_PARSE_OBJECT_ARRAY(ComponentOverrides, IS_OPTIONAL);
		
	END_JSON_SERIALIZE
}
