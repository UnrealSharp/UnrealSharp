#pragma once

#include "CSPropertyType.h"

struct FCSUnrealType
{
	virtual ~FCSUnrealType() = default;
	ECSPropertyType PropertyType = ECSPropertyType::Unknown;
};
