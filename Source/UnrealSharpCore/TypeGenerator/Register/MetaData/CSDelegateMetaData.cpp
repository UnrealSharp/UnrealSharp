#include "CSDelegateMetaData.h"

void FCSDelegateMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FCSTypeReferenceMetaData::SerializeFromJson(JsonObject);
	TSharedPtr<FJsonObject> SignatureFunctionArray = JsonObject->GetObjectField(TEXT("Signature"));
	SignatureFunction.SerializeFromJson(SignatureFunctionArray);
}
