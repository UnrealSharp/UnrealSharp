#include "ReflectionData/CSFieldType.h"

#include "Json/CSJsonMacros.h"
#include "Json/CSJsonUtilities.h"

bool FCSFieldType::Serialize(UnrealSharp::RapidJson::FConstObject JsonObject)
{
	START_JSON_SERIALIZE
		
	CALL_SERIALIZE(FCSUnrealType::Serialize(JsonObject));
	JSON_PARSE_OBJECT(InnerType, IS_REQUIRED);

	END_JSON_SERIALIZE
}
