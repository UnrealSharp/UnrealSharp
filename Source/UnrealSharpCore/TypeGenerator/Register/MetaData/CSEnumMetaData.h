#pragma once

#include "CSTypeReferenceMetaData.h"

struct FCSEnumMetaData : public FCSTypeReferenceMetaData
{
	virtual ~FCSEnumMetaData() = default;

	TArray<FName> Items;

	//FTypeMetaData interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	//End of implementation

	bool operator ==(const FCSEnumMetaData& Other) const
	{
		if (!FCSTypeReferenceMetaData::operator==(Other))
		{
			return false;
		}

		return Items == Other.Items;
	}
};
