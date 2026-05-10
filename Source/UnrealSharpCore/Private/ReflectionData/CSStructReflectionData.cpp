#include "ReflectionData/CSStructReflectionData.h"

#include "Json/CSJsonMacros.h"
#include "Json/CSJsonUtilities.h"
#include "ReflectionData/CSPropertyReflectionData.h"

bool FCSStructReflectionData::Serialize(UnrealSharp::RapidJson::FConstObject JsonObject)
{
	START_JSON_SERIALIZE
		
	CALL_SERIALIZE(FCSTypeReferenceReflectionData::Serialize(JsonObject));
	JSON_PARSE_OBJECT_ARRAY(Properties, IS_OPTIONAL);
		
	END_JSON_SERIALIZE
}
