#include "MetaData/CSEnumMetaData.h"

bool FCSEnumMetaData::Serialize(TSharedPtr<FJsonObject> JsonObject)
{
	START_JSON_SERIALIZE
		
	CALL_SERIALIZE(FCSTypeReferenceMetaData::Serialize(JsonObject));
	JSON_READ_STRING_ARRAY(EnumNames, IS_OPTIONAL);
		
	END_JSON_SERIALIZE
}
