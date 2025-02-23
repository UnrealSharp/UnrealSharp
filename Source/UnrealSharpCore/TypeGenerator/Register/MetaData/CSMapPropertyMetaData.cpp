#include "CSMapPropertyMetaData.h"

#include "TypeGenerator/Register/CSMetaDataUtils.h"

void FCSMapPropertyMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FCSContainerBaseMetaData::SerializeFromJson(JsonObject);
	FCSMetaDataUtils::SerializeProperty(JsonObject->GetObjectField(TEXT("ValueProperty")), ValueType);
}

bool FCSMapPropertyMetaData::IsEqual(TSharedPtr<FCSUnrealType> Other) const
{
	if (!FCSUnrealType::IsEqual(Other))
	{
		return false;
	}

	TSharedPtr<FCSMapPropertyMetaData> OtherMap = SafeCast<FCSMapPropertyMetaData>(Other);
	if (!OtherMap.IsValid())
	{
		return false;
	}

	return ValueType == OtherMap->ValueType && InnerProperty == OtherMap->InnerProperty;
}
