#pragma once

#include "CSFunctionReflectionData.h"
#include "CSStructReflectionData.h"

struct FCSClassBaseReflectionData : FCSStructReflectionData
{
	// FCSReflectionDataBase interface
	virtual bool Serialize(TSharedPtr<FJsonObject> JsonObject) override;
	// End of FCSReflectionDataBase interface
	
	TArray<FCSFunctionReflectionData> Functions;
	EClassFlags ClassFlags;
	FName Config;
};
