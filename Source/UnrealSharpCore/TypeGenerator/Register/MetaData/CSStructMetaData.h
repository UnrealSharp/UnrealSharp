#pragma once

#include "CSTypeReferenceMetaData.h"
#include "TypeGenerator/Register/CSMetaDataUtils.h"

struct FCSStructMetaData : FCSTypeReferenceMetaData
{
	virtual ~FCSStructMetaData() = default;

	TArray<FCSPropertyMetaData> Properties;

	//FTypeMetaData interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	//End of implementation

	bool operator ==(const FCSStructMetaData& Other) const
	{
		if (!FCSTypeReferenceMetaData::operator==(Other))
		{
			return false;
		}

		return Properties == Other.Properties;
	}
};
