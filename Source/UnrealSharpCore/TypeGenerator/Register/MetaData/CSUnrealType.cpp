#include "CSUnrealType.h"

#include "TypeGenerator/Register/CSMetaDataUtils.h"

void FCSUnrealType::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	if (!JsonObject->Values.IsEmpty())
	{
		ArrayDim = JsonObject->GetIntegerField(TEXT("ArrayDim"));
		PropertyType = static_cast<ECSPropertyType>(JsonObject->GetIntegerField(TEXT("PropertyType")));
	}
}
