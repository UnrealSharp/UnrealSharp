#pragma once

#include "CSTypeInfo.h"
#include "UnrealSharpCore/TypeGenerator/Register/CSGeneratedStructBuilder.h"

class FCSGeneratedStructBuilder;

struct UNREALSHARPCORE_API FCSharpStructInfo : TCSharpTypeInfo<FCSStructMetaData, UScriptStruct, FCSGeneratedStructBuilder>
{
	FCSharpStructInfo(const TSharedPtr<FJsonValue>& MetaData) : TCSharpTypeInfo(MetaData) {}
	FCSharpStructInfo() {};
};
