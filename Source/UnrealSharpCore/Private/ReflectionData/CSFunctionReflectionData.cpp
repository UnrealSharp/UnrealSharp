#include "ReflectionData/CSFunctionReflectionData.h"

#include "Json/CSJsonMacros.h"
#include "Json/CSJsonUtilities.h"

bool FCSFunctionReflectionData::Serialize(UnrealSharp::RapidJson::FConstObject JsonObject)
{
	START_JSON_SERIALIZE
		
	CALL_SERIALIZE(FCSStructReflectionData::Serialize(JsonObject));
	JSON_READ_ENUM(FunctionFlags, IS_OPTIONAL);
	JSON_PARSE_OBJECT(ReturnValue, IS_OPTIONAL);
		
	END_JSON_SERIALIZE
}
