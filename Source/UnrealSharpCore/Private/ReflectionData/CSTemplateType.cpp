#include "ReflectionData/CSTemplateType.h"

bool FCSTemplateType::Serialize(TSharedPtr<FJsonObject> JsonObject)
{
	START_JSON_SERIALIZE
		
	CALL_SERIALIZE(FCSUnrealType::Serialize(JsonObject));
	JSON_PARSE_OBJECT_ARRAY(TemplateParameters, IS_REQUIRED);

	END_JSON_SERIALIZE
}
