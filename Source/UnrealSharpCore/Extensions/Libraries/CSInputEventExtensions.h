#pragma once

#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include "CSInputEventExtensions.generated.h"

UCLASS(meta = (InternalType))
class UNREALSHARPCORE_API UCSInputEventExtensions : public UBlueprintFunctionLibrary
{
	GENERATED_BODY()
public:
	UFUNCTION(meta=(ScriptMethod))
	static bool IsRepeat(const FInputEvent& KeyEvent) { return KeyEvent.IsRepeat(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool IsShiftDown(const FInputEvent& KeyEvent) { return KeyEvent.IsShiftDown(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool IsLeftShiftDown(const FInputEvent& KeyEvent) { return KeyEvent.IsLeftShiftDown(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool IsRightShiftDown(const FInputEvent& KeyEvent) { return KeyEvent.IsRightShiftDown(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool IsControlDown(const FInputEvent& KeyEvent) { return KeyEvent.IsControlDown(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool IsLeftControlDown(const FInputEvent& KeyEvent) { return KeyEvent.IsLeftControlDown(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool IsRightControlDown(const FInputEvent& KeyEvent) { return KeyEvent.IsRightControlDown(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool IsAltDown(const FInputEvent& KeyEvent) { return KeyEvent.IsAltDown(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool IsLeftAltDown(const FInputEvent& KeyEvent) { return KeyEvent.IsLeftAltDown(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool IsRightAltDown(const FInputEvent& KeyEvent) { return KeyEvent.IsRightAltDown(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool IsCommandDown(const FInputEvent& KeyEvent) { return KeyEvent.IsCommandDown(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool IsLeftCommandDown(const FInputEvent& KeyEvent) { return KeyEvent.IsLeftCommandDown(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool IsRightCommandDown(const FInputEvent& KeyEvent) { return KeyEvent.IsRightCommandDown(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool AreCapsLocked(const FInputEvent& KeyEvent) { return KeyEvent.AreCapsLocked(); }

	UFUNCTION(meta=(ScriptMethod))
	static uint32 GetUserIndex(const FInputEvent& KeyEvent) { return KeyEvent.GetUserIndex(); }

	UFUNCTION(meta=(ScriptMethod))
	static uint64 GetEventTimestamp(const FInputEvent& KeyEvent) { return KeyEvent.GetEventTimestamp(); }

	UFUNCTION(meta=(ScriptMethod))
	static double GetMillisecondsSinceEvent(const FInputEvent& KeyEvent) { return KeyEvent.GetMillisecondsSinceEvent(); }
};
