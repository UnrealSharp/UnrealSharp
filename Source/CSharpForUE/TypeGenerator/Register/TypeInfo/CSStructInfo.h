#pragma once

#include "CSTypeInfo.h"
#include "CSharpForUE/TypeGenerator/Register/CSGeneratedStructBuilder.h"

class FCSGeneratedStructBuilder;

struct CSHARPFORUE_API FCSharpStructInfo : TCSharpTypeInfo<FCSStructMetaData, UScriptStruct, FCSGeneratedStructBuilder>
{
	FCSharpStructInfo(const TSharedPtr<FJsonValue>& MetaData) : TCSharpTypeInfo(MetaData) {}
	FCSharpStructInfo() {};
};
