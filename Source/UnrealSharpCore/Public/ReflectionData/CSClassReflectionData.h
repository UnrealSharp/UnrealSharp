#pragma once

#include "CSClassBaseReflectionData.h"
#include "CSComponentOverrideReflectionData.h"

struct FCSClassReflectionData : FCSClassBaseReflectionData
{
	// FCSReflectionDataBase interface
	virtual bool Serialize(FConstObject JsonObject) override;
	// End of FCSReflectionDataBase interface

	FCSFieldName ParentClass;
	TArray<FName> Overrides;
	TArray<FCSFieldName> Interfaces;
	TArray<FCSComponentOverrideReflectionData> ComponentOverrides;
};
