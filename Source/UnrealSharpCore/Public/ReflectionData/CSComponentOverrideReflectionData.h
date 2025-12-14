#pragma once

#include "CSReflectionDataBase.h"
#include "CSTypeReferenceReflectionData.h"

struct FCSComponentOverrideReflectionData : FCSReflectionDataBase
{
	// FCSReflectionDataBase interface
	virtual bool Serialize(TSharedPtr<FJsonObject> JsonObject) override;
	// End of FCSReflectionDataBase interface
	
	FCSTypeReferenceReflectionData OwningClass;
	FCSTypeReferenceReflectionData ComponentType;
	FName PropertyName;
};
