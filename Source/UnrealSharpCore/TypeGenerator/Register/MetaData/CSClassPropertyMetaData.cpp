#include "CSClassPropertyMetaData.h"

void FCSClassPropertyMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FCSUnrealType::SerializeFromJson(JsonObject);
	TypeRef.SerializeFromJson(JsonObject->GetObjectField(TEXT("InnerType")));
}

bool FCSClassPropertyMetaData::IsEqual(TSharedPtr<FCSUnrealType> Other) const
{
	if (!FCSUnrealType::IsEqual(Other))
	{
		return false;
	}

	TSharedPtr<FCSClassPropertyMetaData> OtherClass = SafeCast<FCSClassPropertyMetaData>(Other);
	if (!OtherClass.IsValid())
	{
		return false;
	}
	
	return TypeRef == OtherClass->TypeRef;
}

