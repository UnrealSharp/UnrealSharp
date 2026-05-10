#include "ReflectionData/CSDefaultComponentType.h"

#include "Json/CSJsonMacros.h"
#include "Json/CSJsonUtilities.h"

bool FCSDefaultComponentType::Serialize(UnrealSharp::RapidJson::FConstObject JsonObject)
{
	START_JSON_SERIALIZE
		
	CALL_SERIALIZE(FCSFieldType::Serialize(JsonObject));
		
	JSON_READ_BOOL(IsRootComponent, IS_OPTIONAL);
	JSON_READ_STRING(AttachmentComponent, IS_OPTIONAL);
	JSON_READ_STRING(AttachmentSocket, IS_OPTIONAL);

	END_JSON_SERIALIZE
}
