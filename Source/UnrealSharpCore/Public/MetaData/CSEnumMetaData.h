#pragma once

#include "CSTypeReferenceMetaData.h"

struct FCSEnumMetaData : FCSTypeReferenceMetaData
{
	TArray<FName> Items;
};
