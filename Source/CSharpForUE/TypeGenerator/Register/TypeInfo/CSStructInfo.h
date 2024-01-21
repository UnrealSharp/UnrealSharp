#pragma once

#include "CSTypeInfo.h"
#include "CSharpForUE/TypeGenerator/Register/CSGeneratedStructBuilder.h"
#include "CSharpForUE/TypeGenerator/Register/CSMetaData.h"

struct CSHARPFORUE_API FCSharpStructInfo : TCSharpTypeInfo<FStructMetaData, UScriptStruct, FCSGeneratedStructBuilder>
{
	FCSharpStructInfo(const TSharedPtr<FJsonValue>& MetaData) : TCSharpTypeInfo(MetaData) {}
	FCSharpStructInfo() {};
};
