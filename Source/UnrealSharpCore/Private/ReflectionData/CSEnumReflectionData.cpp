#include "ReflectionData/CSEnumReflectionData.h"

bool FCSEnumReflectionData::Serialize(TSharedPtr<FJsonObject> JsonObject)
{
	START_JSON_SERIALIZE
		
	CALL_SERIALIZE(FCSTypeReferenceReflectionData::Serialize(JsonObject));
	JSON_READ_STRING_ARRAY(EnumNames, IS_OPTIONAL);
		
	END_JSON_SERIALIZE
}
