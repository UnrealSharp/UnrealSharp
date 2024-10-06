#include "CSClassPropertyMetaData.h"

void FCSClassPropertyMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FCSUnrealType::SerializeFromJson(JsonObject);
	TypeRef.SerializeFromJson(JsonObject->GetObjectField(TEXT("InnerType")));
}

