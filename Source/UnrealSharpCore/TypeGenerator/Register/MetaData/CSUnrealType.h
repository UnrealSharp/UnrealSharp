#pragma once

#include "CSPropertyType.h"

struct FCSUnrealType
{
	virtual ~FCSUnrealType() = default;

	FName UnrealPropertyClass;
	ECSPropertyType PropertyType = ECSPropertyType::Unknown;
	int32 ArrayDim;

	// Begin FCSUnrealType
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject);
	virtual void OnPropertyCreated(FProperty* Property) {};
	// End FCSUnrealType
};
