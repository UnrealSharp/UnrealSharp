#pragma once

#include "CSPropertyType.h"
#include "CSMetaDataBase.h"

struct FCSUnrealType : FCSMetaDataBase
{
	// FCSMetaDataBase interface
	virtual bool Serialize(TSharedPtr<FJsonObject> JsonObject) override { return true; }
	// End of FCSMetaDataBase interface

	ECSPropertyType PropertyType = ECSPropertyType::Unknown;
};
