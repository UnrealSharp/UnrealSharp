#pragma once

#include "CSTypeInfo.h"
#include "CSharpForUE/TypeGenerator/Register/CSGeneratedInterfaceBuilder.h"
#include "CSharpForUE/TypeGenerator/Register/CSMetaData.h"

struct CSHARPFORUE_API FCSharpInterfaceInfo : TCSharpTypeInfo<FInterfaceMetaData, UClass, FCSGeneratedInterfaceBuilder>
{
	FCSharpInterfaceInfo(const TSharedPtr<FJsonValue>& MetaData) : TCSharpTypeInfo(MetaData) {}
	FCSharpInterfaceInfo() {};
};

