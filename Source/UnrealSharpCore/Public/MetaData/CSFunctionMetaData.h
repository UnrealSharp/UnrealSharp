#pragma once
#include "CSStructMetaData.h"

struct FCSFunctionMetaData : FCSStructMetaData
{
	EFunctionFlags FunctionFlags;
	FCSPropertyMetaData ReturnValue;

	// FCSMetaDataBase interface
	virtual bool Serialize(TSharedPtr<FJsonObject> JsonObject) override
	{
		START_JSON_SERIALIZE
		
		CALL_SERIALIZE(FCSStructMetaData::Serialize(JsonObject));
		JSON_READ_ENUM(FunctionFlags, IS_REQUIRED);
		JSON_PARSE_OBJECT(ReturnValue, IS_OPTIONAL);
		
		END_JSON_SERIALIZE
	}
	// End of FCSMetaDataBase interface

	const FCSPropertyMetaData* TryGetReturnValue() const
	{
		if (!ReturnValue.Type.IsValid())
		{
			return nullptr;
		}

		return &ReturnValue;
	}
};
