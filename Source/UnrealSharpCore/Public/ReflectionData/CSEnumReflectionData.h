#pragma once

#include "CSTypeReferenceReflectionData.h"

struct FCSEnumReflectionData : FCSTypeReferenceReflectionData
{
	// FCSReflectionDataBase interface
	virtual bool Serialize(TSharedPtr<FJsonObject> JsonObject) override;
	// End of FCSReflectionDataBase interface

	TArray<FString> EnumNames;
};
