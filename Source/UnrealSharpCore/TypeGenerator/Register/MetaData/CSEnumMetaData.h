#pragma once

#include "CSTypeReferenceMetaData.h"

struct FCSEnumMetaData : public FCSTypeReferenceMetaData
{
	virtual ~FCSEnumMetaData() = default;

	TArray<FName> Items;

	//FTypeMetaData interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	//End of implementation
};
