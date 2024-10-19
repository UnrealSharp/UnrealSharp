#include "CSDefaultComponentMetaData.h"

void FCSDefaultComponentMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FCSObjectMetaData::SerializeFromJson(JsonObject);
	JsonObject->TryGetBoolField(TEXT("IsRootComponent"), IsRootComponent);

	FString AttachmentComponentStr;
	if (JsonObject->TryGetStringField(TEXT("AttachmentComponent"), AttachmentComponentStr))
	{
		AttachmentComponent = *AttachmentComponentStr;
	}

	FString AttachmentSocketStr;
	if (JsonObject->TryGetStringField(TEXT("AttachmentSocket"), AttachmentSocketStr))
	{
		AttachmentSocket = *AttachmentSocketStr;
	}
}