#include "CSMapPropertyMetaData.h"

#include "TypeGenerator/Register/CSMetaDataUtils.h"

void FCSMapPropertyMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FCSUnrealType::SerializeFromJson(JsonObject);
	FCSMetaDataUtils::SerializeProperty(JsonObject->GetObjectField(TEXT("InnerProperty")), KeyType);
	FCSMetaDataUtils::SerializeProperty(JsonObject->GetObjectField(TEXT("ValueProperty")), ValueType);
}
