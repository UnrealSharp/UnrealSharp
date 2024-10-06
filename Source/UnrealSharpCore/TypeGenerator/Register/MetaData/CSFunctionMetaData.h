#pragma once

#include "CSMemberMetaData.h"
#include "CSPropertyMetaData.h"

struct FCSFunctionMetaData : FCSMemberMetaData
{
	virtual ~FCSFunctionMetaData() = default;

	TArray<FCSPropertyMetaData> Parameters;
	FCSPropertyMetaData ReturnValue;
	bool IsVirtual = false;
	EFunctionFlags FunctionFlags;

	//FTypeMetaData interface implementation
	virtual void SerializeFromJson(const TSharedPtr<FJsonObject>& JsonObject) override;
	//End of implementation
};
