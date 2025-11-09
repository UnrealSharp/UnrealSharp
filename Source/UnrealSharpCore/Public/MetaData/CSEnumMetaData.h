#pragma once

#include "CSTypeReferenceMetaData.h"

struct FCSEnumMetaData : FCSTypeReferenceMetaData
{
	// FCSMetaDataBase interface
	virtual bool Serialize(TSharedPtr<FJsonObject> JsonObject) override;
	// End of FCSMetaDataBase interface

	TArray<FString> EnumNames;
};
