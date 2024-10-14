#pragma once

#include "CSTypeReferenceMetaData.h"
#include "CSUnrealType.h"

struct FCSClassPropertyMetaData : FCSUnrealType
{
	virtual ~FCSClassPropertyMetaData() = default;

	FCSTypeReferenceMetaData TypeRef;

	//FTypeMetaData interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	//End of implementation
};
