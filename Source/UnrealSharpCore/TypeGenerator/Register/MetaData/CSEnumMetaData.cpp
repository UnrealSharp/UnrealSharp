#include "CSEnumMetaData.h"

void FCSEnumMetaData::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	FCSTypeReferenceMetaData::SerializeFromJson(JsonObject);

	const TArray<TSharedPtr<FJsonValue>>* EnumValues;
	if (JsonObject->TryGetArrayField(TEXT("Items"), EnumValues))
	{
		for (const TSharedPtr<FJsonValue>& Item : *EnumValues)
		{
			Items.Add(*Item->AsString());
		}
	}
}