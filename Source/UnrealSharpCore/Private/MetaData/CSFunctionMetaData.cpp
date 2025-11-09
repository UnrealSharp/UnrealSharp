#include "MetaData/CSFunctionMetaData.h"

bool FCSFunctionMetaData::Serialize(TSharedPtr<FJsonObject> JsonObject)
{
	START_JSON_SERIALIZE
		
	CALL_SERIALIZE(FCSStructMetaData::Serialize(JsonObject));
	JSON_READ_ENUM(FunctionFlags, IS_REQUIRED);
	JSON_PARSE_OBJECT(ReturnValue, IS_OPTIONAL);
		
	END_JSON_SERIALIZE
}
