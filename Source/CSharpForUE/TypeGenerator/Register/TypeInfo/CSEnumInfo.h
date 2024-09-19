#pragma once

#include "CSTypeInfo.h"
#include "TypeGenerator/Register/CSGeneratedEnumBuilder.h"
#include "TypeGenerator/Register/MetaData/CSEnumMetaData.h"

struct CSHARPFORUE_API FCSharpEnumInfo : TCSharpTypeInfo<FCSEnumMetaData, UEnum, FCSGeneratedEnumBuilder>
{
	FCSharpEnumInfo(const TSharedPtr<FJsonValue>& MetaData) : TCSharpTypeInfo(MetaData) {}
	FCSharpEnumInfo() {};
};
