#pragma once

#include "CSTypeInfo.h"
#include "UnrealSharpCore/TypeGenerator/Register/CSGeneratedStructBuilder.h"

class FCSGeneratedStructBuilder;

struct UNREALSHARPCORE_API FCSharpStructInfo : TCSharpTypeInfo<FCSStructMetaData, UCSScriptStruct, FCSGeneratedStructBuilder>
{
	FCSharpStructInfo(const TSharedPtr<FJsonValue>& MetaData, const TSharedPtr<FCSAssembly>& InOwningAssembly) : TCSharpTypeInfo(MetaData, InOwningAssembly) {}
	FCSharpStructInfo() {};
};
