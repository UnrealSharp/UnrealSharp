#pragma once

#include "CSPropertyType.h"
#include "FCSMetaDataBase.h"

struct FCSUnrealType : FCSMetaDataBase
{
	ECSPropertyType PropertyType = ECSPropertyType::Unknown;

	// FCSMetaDataBase interface
	virtual bool Serialize(TSharedPtr<FJsonObject> JsonObject) override { return true; }
	// End of FCSMetaDataBase interface
};
