#pragma once

#include "CoreMinimal.h"
#include "CSFieldName.h"
#include "MetaData/CSTypeReferenceMetaData.h"

namespace FCSMetaDataUtils
{
	UNREALSHARPCORE_API void ApplyMetaData(const TArray<FCSMetaDataEntry>& MetaDataMap, UField* Field);
	UNREALSHARPCORE_API void ApplyMetaData(const TArray<FCSMetaDataEntry>& MetaDataMap, FField* Field);
	UNREALSHARPCORE_API FString GetAdjustedFieldName(const FCSFieldName& FieldName);
}
