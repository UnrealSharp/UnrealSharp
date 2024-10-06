#include "CSStructPropertyMetaData.h"

void FCSStructPropertyMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FCSUnrealType::SerializeFromJson(JsonObject);
	TypeRef.SerializeFromJson(JsonObject->GetObjectField(TEXT("InnerType")));
}