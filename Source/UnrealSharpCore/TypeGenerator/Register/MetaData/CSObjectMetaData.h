#pragma once

#include "CSTypeReferenceMetaData.h"
#include "CSUnrealType.h"

struct FCSObjectMetaData : FCSUnrealType
{
	virtual ~FCSObjectMetaData() = default;

	FCSTypeReferenceMetaData InnerType;

	//FTypeMetaData interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	virtual bool IsEqual(TSharedPtr<FCSUnrealType> Other) const override;
	//End of implementation
};
