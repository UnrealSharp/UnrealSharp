#pragma once

#include "CSTypeInfo.h"
#include "TypeGenerator/Register/CSGeneratedEnumBuilder.h"
#include "TypeGenerator/Register/MetaData/CSEnumMetaData.h"

struct UNREALSHARPCORE_API FCSEnumInfo : TCSTypeInfo<FCSEnumMetaData, UCSEnum, FCSGeneratedEnumBuilder>
{
	FCSEnumInfo(const TSharedPtr<FJsonValue>& MetaData, const TSharedPtr<FCSAssembly>& InOwningAssembly) : TCSTypeInfo(MetaData, InOwningAssembly) {}
	FCSEnumInfo() {};
};
