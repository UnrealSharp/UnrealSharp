#include "CSContainerBaseMetaData.h"
#include "TypeGenerator/Register/CSMetaDataUtils.h"

void FCSContainerBaseMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FCSUnrealType::SerializeFromJson(JsonObject);
	FCSMetaDataUtils::SerializeProperty(JsonObject->GetObjectField(TEXT("InnerProperty")), InnerProperty);
}
