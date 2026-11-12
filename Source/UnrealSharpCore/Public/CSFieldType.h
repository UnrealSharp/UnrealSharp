#pragma once

#include "CoreTypes.h"

enum class ECSFieldType : uint8
{
	Unknown,
	Class,
	Struct,
	Enum,
	Interface,
	Delegate,
	MAX
};
