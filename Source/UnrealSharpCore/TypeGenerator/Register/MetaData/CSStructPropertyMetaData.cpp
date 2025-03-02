#include "CSStructPropertyMetaData.h"

void FCSStructPropertyMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FCSUnrealType::SerializeFromJson(JsonObject);
	TypeRef.SerializeFromJson(JsonObject->GetObjectField(TEXT("InnerType")));
}

bool FCSStructPropertyMetaData::IsEqual(TSharedPtr<FCSUnrealType> Other) const
{
	if (!FCSUnrealType::IsEqual(Other))
	{
		return false;
	}

	if (TSharedPtr<FCSStructPropertyMetaData> OtherStructProperty = SafeCast<FCSStructPropertyMetaData>(Other))
	{
		return TypeRef == OtherStructProperty->TypeRef;
	}

	return false;
}
