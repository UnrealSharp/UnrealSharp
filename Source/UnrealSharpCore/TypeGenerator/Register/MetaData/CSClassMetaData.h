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
	
	TArray<FCSTypeReferenceMetaData> Interfaces;

	bool bCanTick = false;
	bool bOverrideInput = false;

	EClassFlags ClassFlags;

	FName ClassConfigName;

	// FTypeReferenceMetaData interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	// End of implementation
	
	bool operator==(const FCSClassMetaData& Other) const
	{
		if (!FCSTypeReferenceMetaData::operator==(Other))
		{
			return false;
		}

		return  ParentClass == Other.ParentClass &&
				Properties == Other.Properties &&
				Functions == Other.Functions &&
				VirtualFunctions == Other.VirtualFunctions &&
				Interfaces == Other.Interfaces &&
				bCanTick == Other.bCanTick &&
				bOverrideInput == Other.bOverrideInput &&
				ClassFlags == Other.ClassFlags &&
				ClassConfigName == Other.ClassConfigName;
	}
};
