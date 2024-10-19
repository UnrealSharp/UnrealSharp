#pragma once

#include "UnrealSharpCore/TypeGenerator/Register/TypeInfo/CSTypeInfo.h"
#include "UnrealSharpCore/TypeGenerator/Register/CSGeneratedInterfaceBuilder.h"

class FCSGeneratedInterfaceBuilder;

struct UNREALSHARPCORE_API FCSharpInterfaceInfo : TCSharpTypeInfo<FCSInterfaceMetaData, UClass, FCSGeneratedInterfaceBuilder>
{
	FCSharpInterfaceInfo(const TSharedPtr<FJsonValue>& MetaData) : TCSharpTypeInfo(MetaData) {}
	FCSharpInterfaceInfo() {};
};

