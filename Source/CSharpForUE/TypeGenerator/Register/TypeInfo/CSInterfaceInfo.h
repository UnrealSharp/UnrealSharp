#pragma once

#include "CSharpForUE/TypeGenerator/Register/TypeInfo/CSTypeInfo.h"
#include "CSharpForUE/TypeGenerator/Register/CSGeneratedInterfaceBuilder.h"
#include "CSharpForUE/TypeGenerator/Register/CSMetaData.h"

class FCSGeneratedInterfaceBuilder;

struct CSHARPFORUE_API FCSharpInterfaceInfo : TCSharpTypeInfo<FInterfaceMetaData, UClass, FCSGeneratedInterfaceBuilder>
{
	FCSharpInterfaceInfo(const TSharedPtr<FJsonValue>& MetaData) : TCSharpTypeInfo(MetaData) {}
	FCSharpInterfaceInfo() {};
};

