#pragma once

#include "CoreMinimal.h"
#include "CSFieldName.h"

namespace FCSMetaDataUtils
{
	UNREALSHARPCORE_API void ApplyMetaData(const TMap<FString, FString>& MetaDataMap, UField* Field);
	UNREALSHARPCORE_API void ApplyMetaData(const TMap<FString, FString>& MetaDataMap, FField* Field);
	UNREALSHARPCORE_API FString GetAdjustedFieldName(const FCSFieldName& FieldName);
}
