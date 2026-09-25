#pragma once

#include "CoreMinimal.h"
#include "CSInteropTypeTraits.h"

// TArray isn't trivially copyable; SysV passes it by hidden reference. Mirrors the managed UnmanagedArray.
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
