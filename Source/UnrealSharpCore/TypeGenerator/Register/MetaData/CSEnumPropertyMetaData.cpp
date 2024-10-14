#include "CSEnumPropertyMetaData.h"

void FCSEnumPropertyMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FCSUnrealType::SerializeFromJson(JsonObject);
	InnerProperty.SerializeFromJson(JsonObject->GetObjectField(TEXT("InnerProperty")));
}