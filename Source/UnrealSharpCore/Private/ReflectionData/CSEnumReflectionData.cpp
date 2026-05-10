#include "ReflectionData/CSEnumReflectionData.h"

#include "Json/CSJsonMacros.h"
#include "Json/CSJsonUtilities.h"

bool FCSEnumReflectionData::Serialize(UnrealSharp::RapidJson::FConstObject JsonObject)
{
	START_JSON_SERIALIZE
		
	CALL_SERIALIZE(FCSTypeReferenceReflectionData::Serialize(JsonObject));
	JSON_READ_STRING_ARRAY(EnumNames, IS_OPTIONAL);
		
	END_JSON_SERIALIZE
}
