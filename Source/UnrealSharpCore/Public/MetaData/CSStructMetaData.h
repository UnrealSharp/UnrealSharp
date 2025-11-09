#pragma once

#include "CSPropertyMetaData.h"
#include "CSTypeReferenceMetaData.h"

struct FCSStructMetaData : FCSTypeReferenceMetaData
{
	// FCSMetaDataBase interface
	virtual bool Serialize(TSharedPtr<FJsonObject> JsonObject) override;
	// End of FCSMetaDataBase interface
	
	TArray<FCSPropertyMetaData> Properties;
};
