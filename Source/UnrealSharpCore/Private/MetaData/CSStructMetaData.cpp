#include "MetaData/CSStructMetaData.h"

bool FCSStructMetaData::Serialize(TSharedPtr<FJsonObject> JsonObject)
{
	START_JSON_SERIALIZE
		
	CALL_SERIALIZE(FCSTypeReferenceMetaData::Serialize(JsonObject));
	JSON_PARSE_OBJECT_ARRAY(Properties, IS_OPTIONAL);
		
	END_JSON_SERIALIZE
}
