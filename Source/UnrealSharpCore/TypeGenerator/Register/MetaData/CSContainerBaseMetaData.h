#pragma once
#include "CSPropertyMetaData.h"

struct FCSContainerBaseMetaData : FCSUnrealType
{
	virtual ~FCSContainerBaseMetaData() = default;

	FCSPropertyMetaData InnerProperty;

	//FTypeMetaData interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	virtual bool IsEqual(TSharedPtr<FCSUnrealType> Other) const override;
	//End of implementation
};
