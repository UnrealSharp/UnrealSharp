#pragma once

#include "CSClassBaseMetaData.h"
#include "CSTypeReferenceMetaData.h"

struct FCSClassMetaData : FCSClassBaseMetaData
{
	TArray<FName> VirtualFunctions;
	TArray<FCSTypeReferenceMetaData> Interfaces;
};
