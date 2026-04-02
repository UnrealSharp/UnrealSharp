#pragma once

#include "CSTypeReferenceReflectionData.h"
#include "CSUnrealType.h"

struct FCSFieldType : FCSUnrealType
{
	// FCSReflectionDataBase interface
	virtual bool Serialize(TSharedPtr<FJsonObject> JsonObject) override;
	// End of FCSReflectionDataBase interface
	
	FCSTypeReferenceReflectionData InnerType;
};
