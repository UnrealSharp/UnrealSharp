#include "CSDelegateMetaData.h"

void FCSDelegateMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FCSUnrealType::SerializeFromJson(JsonObject);
	SignatureFunction.SerializeFromJson(JsonObject->GetObjectField(TEXT("Signature")));
	SignatureFunction.Name = "";
}

bool FCSDelegateMetaData::IsEqual(const TSharedPtr<FCSUnrealType> Other) const
{
	if (!FCSUnrealType::IsEqual(Other))
	{
		return false;
	}

	TSharedPtr<FCSDelegateMetaData> OtherDelegate = SafeCast<FCSDelegateMetaData>(Other);
	if (!OtherDelegate.IsValid())
	{
		return false;
	}

	return SignatureFunction == OtherDelegate->SignatureFunction;
}
