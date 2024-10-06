#include "CSDelegateMetaData.h"

void FCSDelegateMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FCSUnrealType::SerializeFromJson(JsonObject);
	SignatureFunction.SerializeFromJson(JsonObject->GetObjectField(TEXT("Signature")));
	SignatureFunction.Name = "";
}