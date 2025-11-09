#pragma once
#include "CSStructMetaData.h"

struct FCSFunctionMetaData : FCSStructMetaData
{
	// FCSMetaDataBase interface
	virtual bool Serialize(TSharedPtr<FJsonObject> JsonObject) override;
	// End of FCSMetaDataBase interface

	const FCSPropertyMetaData* TryGetReturnValue() const
	{
		if (!ReturnValue.Type.IsValid())
		{
			return nullptr;
		}

		return &ReturnValue;
	}

	EFunctionFlags FunctionFlags;
	FCSPropertyMetaData ReturnValue;
};
