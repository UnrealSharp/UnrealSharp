#include "CSUnrealType.h"

void FCSUnrealType::SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject)
{
	PropertyType = static_cast<ECSPropertyType>(JsonObject->GetIntegerField(TEXT("PropertyType")));
}

bool FCSUnrealType::IsEqual(const TSharedPtr<FCSUnrealType> Other) const
{
	return PropertyType == Other->PropertyType;
}
