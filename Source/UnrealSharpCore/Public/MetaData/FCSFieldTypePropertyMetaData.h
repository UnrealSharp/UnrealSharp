#pragma once

#include "CSTypeReferenceMetaData.h"
#include "CSUnrealType.h"

struct FCSFieldTypePropertyMetaData : FCSUnrealType
{
	FCSTypeReferenceMetaData InnerType;
};
