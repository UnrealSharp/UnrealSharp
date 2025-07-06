#pragma once

#include "CSTypeReferenceMetaData.h"
#include "CSFunctionMetaData.h"

struct FCSDelegateMetaData : public FCSTypeReferenceMetaData
{
	virtual ~FCSDelegateMetaData() = default;

	FCSFunctionMetaData SignatureFunction;

	//FTypeMetaData interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	//End of implementation

	bool operator ==(const FCSDelegateMetaData& Other) const
	{
		if (!FCSTypeReferenceMetaData::operator==(Other))
		{
			return false;
		}

		return SignatureFunction == Other.SignatureFunction;
	}
};
