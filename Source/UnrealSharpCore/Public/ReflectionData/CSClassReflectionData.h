#pragma once

#include "CSClassBaseReflectionData.h"
#include "CSComponentOverrideReflectionData.h"

struct FCSClassReflectionData : FCSClassBaseReflectionData
{
	// FCSReflectionDataBase interface
	virtual bool Serialize(TSharedPtr<FJsonObject> JsonObject) override;
	// End of FCSReflectionDataBase interface

	FCSTypeReferenceReflectionData ParentClass;
	TArray<FName> Overrides;
	TArray<FCSTypeReferenceReflectionData> Interfaces;
	TArray<FCSComponentOverrideReflectionData> ComponentOverrides;
};
