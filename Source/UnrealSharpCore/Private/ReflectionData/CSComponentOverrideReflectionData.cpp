#include "ReflectionData/CSComponentOverrideReflectionData.h"

bool FCSComponentOverrideReflectionData::Serialize(TSharedPtr<FJsonObject> JsonObject)
{
	START_JSON_SERIALIZE
	
	JSON_PARSE_OBJECT(OwningClass, IS_REQUIRED);
	JSON_PARSE_OBJECT(ComponentType, IS_REQUIRED);
	JSON_READ_STRING(PropertyName, IS_OPTIONAL);
	
	END_JSON_SERIALIZE
}
