#include "CSInterfaceMetaData.h"

#include "TypeGenerator/Register/CSMetaDataUtils.h"

void FCSInterfaceMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FCSTypeReferenceMetaData::SerializeFromJson(JsonObject);
	FCSMetaDataUtils::SerializeFunctions(JsonObject->GetArrayField(TEXT("Functions")), Functions);
}
