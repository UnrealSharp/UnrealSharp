#pragma once

#include "CSPropertyReflectionData.h"
#include "CSTypeReferenceReflectionData.h"

struct FCSStructReflectionData : FCSTypeReferenceReflectionData
{
	// FCSReflectionDataBase interface
	virtual bool Serialize(FConstObject JsonObject) override;
	// End of FCSReflectionDataBase interface
	
	TArray<FCSPropertyReflectionData> Properties;
};
