#include "CSObjectMetaData.h"

void FCSObjectMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FCSUnrealType::SerializeFromJson(JsonObject);
	InnerType.SerializeFromJson(JsonObject->GetObjectField(TEXT("InnerType")));
}