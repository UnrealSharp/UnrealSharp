#pragma once

#include "CSUnrealType.h"
#include "CSTypeReferenceMetaData.h"

struct FCSDelegatePropertyMetaData : FCSUnrealType
{
	virtual ~FCSDelegatePropertyMetaData() = default;

	FCSTypeReferenceMetaData Delegate;

	//FTypeMetaData interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	virtual bool IsEqual(TSharedPtr<FCSUnrealType> Other) const override;
	//End of implementation
};
