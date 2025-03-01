#include "CSContainerBaseMetaData.h"
#include "TypeGenerator/Register/CSMetaDataUtils.h"

void FCSContainerBaseMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FCSUnrealType::SerializeFromJson(JsonObject);
	FCSMetaDataUtils::SerializeProperty(JsonObject->GetObjectField(TEXT("InnerProperty")), InnerProperty);
}

bool FCSContainerBaseMetaData::IsEqual(TSharedPtr<FCSUnrealType> Other) const
{
	if (!FCSUnrealType::IsEqual(Other))
	{
		return false;
	}

	TSharedPtr<FCSContainerBaseMetaData> OtherContainer = SafeCast<FCSContainerBaseMetaData>(Other);
	if (!OtherContainer.IsValid())
	{
		return false;
	}

	return InnerProperty == OtherContainer->InnerProperty;
}
