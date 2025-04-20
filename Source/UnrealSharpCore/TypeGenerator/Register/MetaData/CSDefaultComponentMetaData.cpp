#include "CSDefaultComponentMetaData.h"

#include "UnrealSharpCore.h"

bool FCSDefaultComponentMetaData::HasValidAttachment() const
{
	return AttachmentComponent != NAME_None;
}

void FCSDefaultComponentMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FCSObjectMetaData::SerializeFromJson(JsonObject);
	JsonObject->TryGetBoolField(TEXT("IsRootComponent"), IsRootComponent);

	FString AttachmentComponentStr;
	if (JsonObject->TryGetStringField(TEXT("AttachmentComponent"), AttachmentComponentStr))
	{
		if (!AttachmentComponentStr.IsEmpty())
		{
			if (!IsRootComponent)
			{
				AttachmentComponent = *AttachmentComponentStr;
			}
			else
			{
				UE_LOG(LogUnrealSharp, Error, TEXT("Root component %s cannot have an attachment component!"), *AttachmentComponentStr);
			}
		}
	}

	FString AttachmentSocketStr;
	if (JsonObject->TryGetStringField(TEXT("AttachmentSocket"), AttachmentSocketStr))
	{
		if (!AttachmentSocketStr.IsEmpty())
		{
			if (!IsRootComponent)
			{
				AttachmentSocket = *AttachmentSocketStr;
			}
			else
			{
				UE_LOG(LogUnrealSharp, Error, TEXT("Root component %s cannot have an attachment socket!"), *AttachmentSocketStr);
			}
		}
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
