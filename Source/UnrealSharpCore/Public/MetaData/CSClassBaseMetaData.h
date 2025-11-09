#pragma once

#include "CSFunctionMetaData.h"
#include "CSStructMetaData.h"

struct FCSClassBaseMetaData : FCSStructMetaData
{
	// FCSMetaDataBase interface
	virtual bool Serialize(TSharedPtr<FJsonObject> JsonObject) override;
	// End of FCSMetaDataBase interface
	
	FCSTypeReferenceMetaData ParentClass;
	TArray<FCSFunctionMetaData> Functions;
	EClassFlags ClassFlags;
	FName ConfigName;
};
