#pragma once
#include "CSPropertyMetaData.h"

struct FCSTemplateType : FCSUnrealType
{
	TArray<FCSPropertyMetaData> TemplateParameters;

	// FCSMetaDataBase interface
	virtual bool Serialize(TSharedPtr<FJsonObject> JsonObject) override
	{
		START_JSON_SERIALIZE
		
		CALL_SERIALIZE(FCSUnrealType::Serialize(JsonObject));
		JSON_PARSE_OBJECT_ARRAY(TemplateParameters, IS_REQUIRED);

		END_JSON_SERIALIZE
	}
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
};
