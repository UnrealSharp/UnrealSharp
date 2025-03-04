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

bool FCSDefaultComponentMetaData::IsEqual(TSharedPtr<FCSUnrealType> Other) const
{
	if (!FCSUnrealType::IsEqual(Other))
	{
		return false;
	}

	TSharedPtr<FCSDefaultComponentMetaData> OtherDefaultComponent = SafeCast<FCSDefaultComponentMetaData>(Other);
	if (!OtherDefaultComponent.IsValid())
	{
		return false;
	}

	return IsRootComponent == OtherDefaultComponent->IsRootComponent &&
		AttachmentComponent == OtherDefaultComponent->AttachmentComponent &&
		AttachmentSocket == OtherDefaultComponent->AttachmentSocket &&
			InnerType == OtherDefaultComponent->InnerType;
}
