#pragma once

#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include "CSKeyEventExtensions.generated.h"

UCLASS(meta = (InternalType))
class UNREALSHARPCORE_API UCSKeyEventExtensions : public UBlueprintFunctionLibrary
{
	GENERATED_BODY()
public:
	UFUNCTION(meta=(ScriptMethod))
	static FKey GetKey(const FKeyEvent& KeyEvent) { return KeyEvent.GetKey(); }

	UFUNCTION(meta=(ScriptMethod))
	static uint32 GetCharacter(const FKeyEvent& KeyEvent) { return KeyEvent.GetCharacter(); }

	UFUNCTION(meta=(ScriptMethod))
	static uint32 GetKeyCode(const FKeyEvent& KeyEvent) { return KeyEvent.GetKeyCode(); }

	UFUNCTION(meta=(ScriptMethod))
	static FText ToText(const FKeyEvent& KeyEvent) { return KeyEvent.ToText(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool IsKeyEvent(const FKeyEvent& KeyEvent) { return KeyEvent.IsKeyEvent(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool IsRepeat(const FKeyEvent& KeyEvent) { return KeyEvent.IsRepeat(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool IsShiftDown(const FKeyEvent& KeyEvent) { return KeyEvent.IsShiftDown(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool IsLeftShiftDown(const FKeyEvent& KeyEvent) { return KeyEvent.IsLeftShiftDown(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool IsRightShiftDown(const FKeyEvent& KeyEvent) { return KeyEvent.IsRightShiftDown(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool IsControlDown(const FKeyEvent& KeyEvent) { return KeyEvent.IsControlDown(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool IsLeftControlDown(const FKeyEvent& KeyEvent) { return KeyEvent.IsLeftControlDown(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool IsRightControlDown(const FKeyEvent& KeyEvent) { return KeyEvent.IsRightControlDown(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool IsAltDown(const FKeyEvent& KeyEvent) { return KeyEvent.IsAltDown(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool IsLeftAltDown(const FKeyEvent& KeyEvent) { return KeyEvent.IsLeftAltDown(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool IsRightAltDown(const FKeyEvent& KeyEvent) { return KeyEvent.IsRightAltDown(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool IsCommandDown(const FKeyEvent& KeyEvent) { return KeyEvent.IsCommandDown(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool IsLeftCommandDown(const FKeyEvent& KeyEvent) { return KeyEvent.IsLeftCommandDown(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool IsRightCommandDown(const FKeyEvent& KeyEvent) { return KeyEvent.IsRightCommandDown(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool AreCapsLocked(const FKeyEvent& KeyEvent) { return KeyEvent.AreCapsLocked(); }

	UFUNCTION(meta=(ScriptMethod))
	static uint32 GetUserIndex(const FKeyEvent& KeyEvent) { return KeyEvent.GetUserIndex(); }
};
