#pragma once

#include "CSTypeReferenceMetaData.h"
#include "CSFunctionMetaData.h"

struct FCSPropertyMetaData;

struct FCSClassMetaData : public FCSTypeReferenceMetaData
{
	virtual ~FCSClassMetaData() = default;

	FCSTypeReferenceMetaData ParentClass;
	
	TArray<FCSPropertyMetaData> Properties;
	
	TArray<FCSFunctionMetaData> Functions;
	TArray<FName> VirtualFunctions;
	
	TArray<FName> Interfaces;

	bool bCanTick = false;
	bool bOverrideInput = false;

	EClassFlags ClassFlags;

	FName ClassConfigName;

	// FTypeReferenceMetaData interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	// End of implementation
};
