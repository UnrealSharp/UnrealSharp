#pragma once

#include "CoreMinimal.h"
#include "CSInteropTypeTraits.h"

struct FGCHandleIntPtr
{
	bool operator==(const FGCHandleIntPtr& Other) const = default;
	uint8* ManagedHandlePtr = nullptr;
};

static_assert(sizeof(FGCHandleIntPtr) == sizeof(void*));
CS_ASSERT_INTEROP_SAFE_TYPE(FGCHandleIntPtr);
