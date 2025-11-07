#pragma once
#include "CSStructMetaData.h"

struct FCSFunctionMetaData : FCSStructMetaData
{
	EFunctionFlags Flags;

	const FCSPropertyMetaData* TryGetReturnValue() const
	{
		if (Properties.Num() > 0 && Properties[0].Flags & CPF_ReturnParm)
		{
			return &Properties[0];
		}
		
		return nullptr;
	}
};
