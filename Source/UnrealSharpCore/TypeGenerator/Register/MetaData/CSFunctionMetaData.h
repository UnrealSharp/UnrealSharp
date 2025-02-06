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

	bool HasReturnValue() const { return ReturnValue.Type != nullptr; }

	bool operator==(const FCSFunctionMetaData& Other) const
	{
		if (!FCSMemberMetaData::operator==(Other))
		{
			return false;
		}

		return  Parameters == Other.Parameters &&
				(!HasReturnValue() || ReturnValue == Other.ReturnValue) &&
				IsVirtual == Other.IsVirtual &&
				FunctionFlags == Other.FunctionFlags;
	}
};
