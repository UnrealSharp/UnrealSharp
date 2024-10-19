#pragma once

#include "CSTypeReferenceMetaData.h"
#include "CSUnrealType.h"

struct FCSEnumPropertyMetaData : FCSUnrealType
{
	virtual ~FCSEnumPropertyMetaData() = default;

	FCSTypeReferenceMetaData InnerProperty;

	// FUnrealType interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	// End of implementation
};
