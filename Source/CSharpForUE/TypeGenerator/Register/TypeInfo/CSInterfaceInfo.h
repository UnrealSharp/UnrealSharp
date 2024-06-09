#pragma once

#include "CSharpForUE/TypeGenerator/Register/TypeInfo/CSTypeInfo.h"
#include "CSharpForUE/TypeGenerator/Register/CSGeneratedInterfaceBuilder.h"

class FCSGeneratedInterfaceBuilder;

struct CSHARPFORUE_API FCSharpInterfaceInfo : TCSharpTypeInfo<FCSInterfaceMetaData, UClass, FCSGeneratedInterfaceBuilder>
{
	FCSharpInterfaceInfo(const TSharedPtr<FJsonValue>& MetaData) : TCSharpTypeInfo(MetaData) {}
	FCSharpInterfaceInfo() {};
};

