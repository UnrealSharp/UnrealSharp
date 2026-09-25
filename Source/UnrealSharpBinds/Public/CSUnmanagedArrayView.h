#pragma once

#include "CoreMinimal.h"
#include "CSInteropTypeTraits.h"

/**
 * Trivially copyable view of a TArray ({ Data, ArrayNum, ArrayMax }), matching the managed UnmanagedArray struct.
 * Pass this instead of a TArray by value: TArray is not trivially copyable, so SysV x86-64 and AArch64 pass it by
 * hidden reference while .NET passes the 16-byte managed struct in registers.
 */
struct FCSUnmanagedArrayView
{
	const void* Data = nullptr;
	int32 ArrayNum = 0;
	int32 ArrayMax = 0;

	template <typename ElementType, typename AllocatorType>
	static FCSUnmanagedArrayView FromArray(const TArray<ElementType, AllocatorType>& Array)
	{
		return { Array.GetData(), Array.Num(), Array.Max() };
	}
};

CS_ASSERT_INTEROP_SAFE_TYPE(FCSUnmanagedArrayView);
static_assert(sizeof(FCSUnmanagedArrayView) == sizeof(FScriptArray), "FCSUnmanagedArrayView must match the TArray layout.");
