#include "ReflectionData/CSTemplateType.h"

#include "Json/CSJsonMacros.h"
#include "Json/CSJsonUtilities.h"

bool FCSTemplateType::Serialize(UnrealSharp::RapidJson::FConstObject JsonObject)
{
	START_JSON_SERIALIZE
		
	CALL_SERIALIZE(FCSUnrealType::Serialize(JsonObject));
	JSON_PARSE_OBJECT_ARRAY(TemplateParameters, IS_REQUIRED);

	END_JSON_SERIALIZE
}
