#include "MetaData/CSFieldTypePropertyMetaData.h"

bool FCSFieldTypePropertyMetaData::Serialize(TSharedPtr<FJsonObject> JsonObject)
{
	START_JSON_SERIALIZE
		
	CALL_SERIALIZE(FCSUnrealType::Serialize(JsonObject));
	JSON_PARSE_OBJECT(InnerType, IS_REQUIRED);

	END_JSON_SERIALIZE
}
