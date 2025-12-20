#pragma once

#include "CoreMinimal.h"
#include "Containers/StringConv.h"
#include "CSBindsManager.h"
#include "FStringExporter.generated.h"

UCLASS()
class UFStringExporter : public UObject
{
	GENERATED_BODY()
public:
	UNREALSHARP_FUNCTION()
	static void MarshalToNativeString(FString* NativeString, const char* ManagedString)
	{
		if (!NativeString)
		{
			return;
		}

		if (!ManagedString)
		{
			*NativeString = FString();
			return;
		}

		// Contract: this API receives a UTF-8 byte string and must decode it into TCHAR.
		// NOTE: do NOT call this from C# by passing a string directly (the runtime will marshal it as ANSI and replace non-ASCII with '?').
		// Prefer MarshalToNativeStringView (UTF-16 pointer + length) to avoid data loss.
		*NativeString = UTF8_TO_TCHAR(ManagedString);
	}

	UNREALSHARP_FUNCTION()
	static void MarshalToNativeStringView(FString* NativeString, const UTF16CHAR* ManagedString, int32 Length)
	{
		if (!NativeString)
		{
			return;
		}

		if (!ManagedString || Length <= 0)
		{
			*NativeString = FString();
			return;
		}

		// C# strings are UTF-16 (char*). Use a length-based view (no null-termination dependency) and convert to TCHAR correctly.
		const auto Converted = StringCast<TCHAR>(ManagedString, Length);
		*NativeString = FString(Converted.Length(), Converted.Get());
	}
};
