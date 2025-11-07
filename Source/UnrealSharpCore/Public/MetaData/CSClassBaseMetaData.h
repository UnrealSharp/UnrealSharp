#pragma once

#include "CSFunctionMetaData.h"
#include "CSStructMetaData.h"

struct FCSClassBaseMetaData : FCSStructMetaData
{
	FCSTypeReferenceMetaData ParentClass;
	TArray<FCSFunctionMetaData> Functions;
	EClassFlags ClassFlags;
	FName ConfigName;
};
