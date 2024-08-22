#pragma once

#include "CSPropertyMetaData.h"
#include "CSUnrealType.h"

struct FCSArrayPropertyMetaData : FCSUnrealType
{
	virtual ~FCSArrayPropertyMetaData() = default;

	FCSPropertyMetaData InnerProperty;

	//FTypeMetaData interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	//End of implementation
};
