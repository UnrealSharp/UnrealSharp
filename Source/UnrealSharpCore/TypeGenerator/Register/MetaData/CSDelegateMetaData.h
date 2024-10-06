#pragma once

#include "CSUnrealType.h"
#include "CSFunctionMetaData.h"

struct FCSDelegateMetaData : FCSUnrealType
{
	virtual ~FCSDelegateMetaData() = default;

	FCSFunctionMetaData SignatureFunction;

	//FTypeMetaData interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	//End of implementation
};
