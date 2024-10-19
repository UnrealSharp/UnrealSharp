#include "CSMapPropertyMetaData.h"

#include "TypeGenerator/Register/CSMetaDataUtils.h"

void FCSMapPropertyMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FCSContainerBaseMetaData::SerializeFromJson(JsonObject);
	FCSMetaDataUtils::SerializeProperty(JsonObject->GetObjectField(TEXT("ValueProperty")), ValueType);
}
