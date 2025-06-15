#include "CSDelegatePropertyMetaData.h"

void FCSDelegatePropertyMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FCSUnrealType::SerializeFromJson(JsonObject);

	TSharedPtr<FJsonObject> DelegateObject = JsonObject->GetObjectField(TEXT("UnrealDelegateType"));
	Delegate.SerializeFromJson(DelegateObject);
}

bool FCSDelegatePropertyMetaData::IsEqual(const TSharedPtr<FCSUnrealType> Other) const
{
	if (!FCSUnrealType::IsEqual(Other))
	{
		return false;
	}

	TSharedPtr<FCSDelegatePropertyMetaData> OtherDelegate = SafeCast<FCSDelegatePropertyMetaData>(Other);
	if (!OtherDelegate.IsValid())
	{
		return false;
	}

	return Delegate == OtherDelegate->Delegate;
}
