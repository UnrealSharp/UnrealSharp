#pragma once

#include "CSTypeInfo.h"
#include "UObject/Class.h"
#include "TypeGenerator/Register/CSGeneratedDelegateBuilder.h"
#include "TypeGenerator/Register/MetaData/CSDelegateMetaData.h"

struct UNREALSHARPCORE_API FCSDelegateInfo : TCSTypeInfo<FCSDelegateMetaData, UDelegateFunction, FCSGeneratedDelegateBuilder>
{
	FCSDelegateInfo(const TSharedPtr<FJsonValue>& MetaData, const TSharedPtr<FCSAssembly>& InOwningAssembly) : TCSTypeInfo(MetaData, InOwningAssembly) {}
	FCSDelegateInfo() {};
};
