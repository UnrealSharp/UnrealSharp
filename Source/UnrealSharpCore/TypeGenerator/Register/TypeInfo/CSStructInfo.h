#pragma once

#include "CSTypeInfo.h"
#include "UnrealSharpCore/TypeGenerator/Register/CSGeneratedStructBuilder.h"

class FCSGeneratedStructBuilder;

struct UNREALSHARPCORE_API FCSStructInfo : TCSTypeInfo<FCSStructMetaData, UCSScriptStruct, FCSGeneratedStructBuilder>
{
	FCSStructInfo(const TSharedPtr<FJsonValue>& MetaData, const TSharedPtr<FCSAssembly>& InOwningAssembly) : TCSTypeInfo(MetaData, InOwningAssembly) {}
	FCSStructInfo() {};
};
