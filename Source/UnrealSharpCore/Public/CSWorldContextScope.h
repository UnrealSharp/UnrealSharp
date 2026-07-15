#pragma once

#include "CoreMinimal.h"

/**
 * World context for the duration of a native-to-managed call.
 * Contexts are stored per thread and restored when the scope exits, preventing
 * nested calls from leaking their world into their caller.
 */
class UNREALSHARPCORE_API FCSWorldContextScope
{
public:
	explicit FCSWorldContextScope(UObject* WorldContextObject, bool bInheritIfUnresolved = true);
	~FCSWorldContextScope();

	FCSWorldContextScope(const FCSWorldContextScope&) = delete;
	FCSWorldContextScope& operator=(const FCSWorldContextScope&) = delete;
};
