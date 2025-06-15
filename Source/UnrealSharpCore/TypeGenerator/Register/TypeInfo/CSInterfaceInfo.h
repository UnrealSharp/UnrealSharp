#pragma once

#include "UnrealSharpCore/TypeGenerator/Register/TypeInfo/CSTypeInfo.h"
#include "UnrealSharpCore/TypeGenerator/Register/CSGeneratedInterfaceBuilder.h"

class FCSGeneratedInterfaceBuilder;

struct UNREALSHARPCORE_API FCSInterfaceInfo : TCSTypeInfo<FCSInterfaceMetaData, UClass, FCSGeneratedInterfaceBuilder>
{
	FCSInterfaceInfo(const TSharedPtr<FJsonValue>& MetaData, const TSharedPtr<FCSAssembly>& InOwningAssembly) : TCSTypeInfo(MetaData, InOwningAssembly) {}
	FCSInterfaceInfo() {};
};

