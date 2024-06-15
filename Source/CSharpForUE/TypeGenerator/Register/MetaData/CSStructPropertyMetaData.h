#pragma once

#include "CSTypeReferenceMetaData.h"
#include "CSUnrealType.h"

struct FCSStructPropertyMetaData : FCSUnrealType
{
	virtual ~FCSStructPropertyMetaData() = default;

	FCSTypeReferenceMetaData TypeRef;

	// FUnrealType interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	// End of implementation
};
