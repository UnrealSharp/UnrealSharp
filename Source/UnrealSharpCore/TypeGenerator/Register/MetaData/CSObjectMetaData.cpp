#include "CSObjectMetaData.h"

void FCSObjectMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FCSUnrealType::SerializeFromJson(JsonObject);
	InnerType.SerializeFromJson(JsonObject->GetObjectField(TEXT("InnerType")));
}

bool FCSObjectMetaData::IsEqual(TSharedPtr<FCSUnrealType> Other) const
{
	if (!FCSUnrealType::IsEqual(Other))
	{
		return false;
	}

	TSharedPtr<FCSObjectMetaData> OtherObject = SafeCast<FCSObjectMetaData>(Other);
	if (!OtherObject.IsValid())
	{
		return false;
	}

	return InnerType == OtherObject->InnerType;
}
