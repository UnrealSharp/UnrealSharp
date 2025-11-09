#include "MetaData/CSDefaultComponentMetaData.h"

bool FCSDefaultComponentMetaData::Serialize(TSharedPtr<FJsonObject> JsonObject)
{
	START_JSON_SERIALIZE
		
	CALL_SERIALIZE(FCSFieldTypePropertyMetaData::Serialize(JsonObject));
		
	JSON_READ_BOOL(IsRootComponent, IS_REQUIRED);
	JSON_READ_STRING(AttachmentComponent, IS_REQUIRED);
	JSON_READ_STRING(AttachmentSocket, IS_REQUIRED);

	END_JSON_SERIALIZE
}
