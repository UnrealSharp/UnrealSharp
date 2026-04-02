#pragma once

#include "CSPropertyType.h"
#include "CSReflectionDataBase.h"

struct FCSUnrealType : FCSReflectionDataBase
{
	// FCSReflectionDataBase interface
	virtual bool Serialize(TSharedPtr<FJsonObject> JsonObject) override { return true; }
	// End of FCSReflectionDataBase interface

	ECSPropertyType PropertyType = ECSPropertyType::Unknown;
};
