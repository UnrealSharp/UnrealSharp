#pragma once

#include "CSContainerBaseMetaData.h"
#include "CSPropertyMetaData.h"

struct FCSMapPropertyMetaData : public FCSContainerBaseMetaData
{
	FCSPropertyMetaData ValueType;

	// FTypeMetaData interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	// End of implementation
};
