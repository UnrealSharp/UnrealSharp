#pragma once

#include "CSFunctionReflectionData.h"
#include "CSStructReflectionData.h"
#include "Json/CSRapidJsonUtilties.h"

struct FCSClassBaseReflectionData : FCSStructReflectionData
{
	// FCSReflectionDataBase interface
	virtual bool Serialize(FConstObject JsonObject) override;
	// End of FCSReflectionDataBase interface
	
	TArray<FCSFunctionReflectionData> Functions;
	EClassFlags ClassFlags;
	FName Config;
};
