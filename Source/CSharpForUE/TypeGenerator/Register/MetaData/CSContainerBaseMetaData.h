#pragma once
#include "CSPropertyMetaData.h"

struct FCSContainerBaseMetaData : FCSUnrealType
{
	virtual ~FCSContainerBaseMetaData() = default;

	FCSPropertyMetaData InnerProperty;

	//FTypeMetaData interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	//End of implementation
};
