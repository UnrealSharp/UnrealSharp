#pragma once

#include "CSTypeReferenceMetaData.h"
#include "CSFunctionMetaData.h"

struct FCSInterfaceMetaData : FCSTypeReferenceMetaData
{
	virtual ~FCSInterfaceMetaData() = default;

	TArray<FCSFunctionMetaData> Functions;
	
	//FTypeMetaData interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	//End of implementation
};
