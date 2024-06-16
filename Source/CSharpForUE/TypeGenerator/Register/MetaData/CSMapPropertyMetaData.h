#pragma once

#include "CSPropertyMetaData.h"
#include "CSUnrealType.h"

struct FCSMapPropertyMetaData : public FCSUnrealType
{
	FCSPropertyMetaData KeyType;
	FCSPropertyMetaData ValueType;

	// FTypeMetaData interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	// End of implementation
};
