#include "CSArrayPropertyMetaData.h"

#include "TypeGenerator/Register/CSMetaDataUtils.h"

void FCSArrayPropertyMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FCSUnrealType::SerializeFromJson(JsonObject);
	FCSMetaDataUtils::SerializeProperty(JsonObject->GetObjectField(TEXT("InnerProperty")), InnerProperty);
}
