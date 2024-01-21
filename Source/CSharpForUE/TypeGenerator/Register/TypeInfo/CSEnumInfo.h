#pragma once

#include "CSTypeInfo.h"
#include "CSharpForUE/TypeGenerator/Register/CSGeneratedEnumBuilder.h"
#include "CSharpForUE/TypeGenerator/Register/CSMetaData.h"

struct CSHARPFORUE_API FCSharpEnumInfo : TCSharpTypeInfo<FEnumMetaData, UEnum, FCSGeneratedEnumBuilder>
{
	FCSharpEnumInfo(const TSharedPtr<FJsonValue>& MetaData) : TCSharpTypeInfo(MetaData) {}
	FCSharpEnumInfo() {};
};
