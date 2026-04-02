#pragma once
#include "CSPropertyReflectionData.h"

struct FCSTemplateType : FCSUnrealType
{
	// FCSReflectionDataBase interface
	virtual bool Serialize(TSharedPtr<FJsonObject> JsonObject) override;
	// End of FCSReflectionDataBase interface

	const FCSPropertyReflectionData* GetTemplateArgument(int32 Index) const
	{
		if (TemplateParameters.IsValidIndex(Index))
		{
			return &TemplateParameters[Index];
		}

		ensureMsgf(false, TEXT("Template parameter index out of bounds: %d"), Index);
		return nullptr;
	}

	TArray<FCSPropertyReflectionData> TemplateParameters;
};
