#pragma once

#include "CSStructReflectionData.h"

struct FCSFunctionReflectionData : FCSStructReflectionData
{
	// FCSReflectionDataBase interface
	virtual bool Serialize(FConstObject JsonObject) override;
	// End of FCSReflectionDataBase interface

	const FCSPropertyReflectionData* TryGetReturnValue() const
	{
		if (!ReturnValue.InnerType.IsValid())
		{
			return nullptr;
		}

		return &ReturnValue;
	}

	EFunctionFlags FunctionFlags;
	FCSPropertyReflectionData ReturnValue;
};
