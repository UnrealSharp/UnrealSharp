#pragma once
#include "CSPropertyMetaData.h"

struct FCSTemplateType : FCSUnrealType
{
	// FCSMetaDataBase interface
	virtual bool Serialize(TSharedPtr<FJsonObject> JsonObject) override;
	// End of FCSMetaDataBase interface

	const FCSPropertyMetaData* GetTemplateArgument(int32 Index) const
	{
		if (TemplateParameters.IsValidIndex(Index))
		{
			return &TemplateParameters[Index];
		}

		ensureMsgf(false, TEXT("Template parameter index out of bounds: %d"), Index);
		return nullptr;
	}

	TArray<FCSPropertyMetaData> TemplateParameters;
};
