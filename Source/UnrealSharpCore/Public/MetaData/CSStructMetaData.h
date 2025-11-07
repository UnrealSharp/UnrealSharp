#pragma once

#include "CSPropertyMetaData.h"
#include "CSTypeReferenceMetaData.h"

struct FCSStructMetaData : FCSTypeReferenceMetaData
{
	TArray<FCSPropertyMetaData> Properties;
};
