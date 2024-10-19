#include "CSStructMetaData.h"

void FCSStructMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FCSTypeReferenceMetaData::SerializeFromJson(JsonObject);
	const TArray<TSharedPtr<FJsonValue>>* FoundProperties;
	if (JsonObject->TryGetArrayField(TEXT("Fields"), FoundProperties))
	{
		FCSMetaDataUtils::SerializeProperties(*FoundProperties, Properties);
	}
}