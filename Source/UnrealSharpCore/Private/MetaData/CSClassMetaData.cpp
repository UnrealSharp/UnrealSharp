#include "MetaData/CSClassMetaData.h"

bool FCSClassMetaData::Serialize(TSharedPtr<FJsonObject> JsonObject)
{
	START_JSON_SERIALIZE
		
	CALL_SERIALIZE(FCSClassBaseMetaData::Serialize(JsonObject));
	JSON_READ_STRING_ARRAY(Overrides, IS_OPTIONAL);
	JSON_PARSE_OBJECT_ARRAY(Interfaces, IS_OPTIONAL);
		
	END_JSON_SERIALIZE
}
