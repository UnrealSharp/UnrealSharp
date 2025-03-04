#pragma once

#include "CSTypeInfo.h"
#include "TypeGenerator/Register/CSGeneratedEnumBuilder.h"
#include "TypeGenerator/Register/MetaData/CSEnumMetaData.h"

struct UNREALSHARPCORE_API FCSharpEnumInfo : TCSharpTypeInfo<FCSEnumMetaData, UCSEnum, FCSGeneratedEnumBuilder>
{
	FCSharpEnumInfo(const TSharedPtr<FJsonValue>& MetaData, const TSharedPtr<FCSAssembly>& InOwningAssembly) : TCSharpTypeInfo(MetaData, InOwningAssembly) {}
	FCSharpEnumInfo() {};
};
