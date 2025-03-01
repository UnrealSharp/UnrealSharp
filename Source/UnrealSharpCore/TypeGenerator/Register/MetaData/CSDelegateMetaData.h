#pragma once

#include "CSUnrealType.h"
#include "CSFunctionMetaData.h"

struct FCSDelegateMetaData : FCSUnrealType
{
	virtual ~FCSDelegateMetaData() = default;

	FCSFunctionMetaData SignatureFunction;

	//FTypeMetaData interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	virtual bool IsEqual(TSharedPtr<FCSUnrealType> Other) const override;
	//End of implementation
};
