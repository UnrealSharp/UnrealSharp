#include "CSEnumPropertyMetaData.h"

void FCSEnumPropertyMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FCSUnrealType::SerializeFromJson(JsonObject);
	InnerProperty.SerializeFromJson(JsonObject->GetObjectField(TEXT("InnerProperty")));
}

bool FCSEnumPropertyMetaData::IsEqual(TSharedPtr<FCSUnrealType> Other) const
{
	if (!FCSUnrealType::IsEqual(Other))
	{
		return false;
	}

	TSharedPtr<FCSEnumPropertyMetaData> OtherEnum = SafeCast<FCSEnumPropertyMetaData>(Other);
	if (!OtherEnum.IsValid())
	{
		return false;
	}

	return InnerProperty == OtherEnum->InnerProperty;
}
