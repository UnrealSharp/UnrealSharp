#pragma once
#include "CSPropertyMetaData.h"

struct FCSTemplateType : FCSUnrealType
{
	TArray<FCSPropertyMetaData> TemplateParameters;

	const FCSPropertyMetaData* GetTemplateArgument(int32 Index) const
	{
		if (TemplateParameters.IsValidIndex(Index))
		{
			return &TemplateParameters[Index];
		}

		ensureMsgf(false, TEXT("Template parameter index out of bounds: %d"), Index);
		return nullptr;
	}
};
