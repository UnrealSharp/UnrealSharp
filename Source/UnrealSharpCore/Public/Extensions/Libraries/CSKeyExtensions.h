#pragma once

#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include "CSKeyExtensions.generated.h"

UCLASS(meta = (InternalType))
class UCSKeyExtensions : public UBlueprintFunctionLibrary
{
	GENERATED_BODY()
public:
	UFUNCTION(meta=(ScriptMethod))
	static bool IsValid(const FKey& Key) { return Key.IsValid(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool IsModifierKey(const FKey& Key) { return Key.IsModifierKey(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool IsGamepadKey(const FKey& Key) { return Key.IsGamepadKey(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool IsTouch(const FKey& Key) { return Key.IsTouch(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool IsMouseButton(const FKey& Key) { return Key.IsMouseButton(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool IsButtonAxis(const FKey& Key) { return Key.IsButtonAxis(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool IsAxis1D(const FKey& Key) { return Key.IsAxis1D(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool IsAxis2D(const FKey& Key) { return Key.IsAxis2D(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool IsAxis3D(const FKey& Key) { return Key.IsAxis3D(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool IsDigital(const FKey& Key) { return Key.IsDigital(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool IsAnalog(const FKey& Key) { return Key.IsAnalog(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool IsBindableInBlueprints(const FKey& Key) { return Key.IsBindableInBlueprints(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool ShouldUpdateAxisWithoutSamples(const FKey& Key) { return Key.ShouldUpdateAxisWithoutSamples(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool IsBindableToActions(const FKey& Key) { return Key.IsBindableToActions(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool IsDeprecated(const FKey& Key) { return Key.IsDeprecated(); }

	UFUNCTION(meta=(ScriptMethod))
	static bool IsGesture(const FKey& Key) { return Key.IsGesture(); }

	UFUNCTION(meta=(ScriptMethod))
	static FText GetDisplayName(const FKey& Key, bool bLongDisplayName = true) { return Key.GetDisplayName(bLongDisplayName); }

	UFUNCTION(meta=(ScriptMethod))
	static FString ToString(const FKey& Key) { return Key.ToString(); }

	UFUNCTION(meta=(ScriptMethod))
	static FName GetMenuCategory(const FKey& Key) { return Key.GetMenuCategory(); }

	UFUNCTION(meta=(ScriptMethod))
	static FKey GetPairedAxisKey(const FKey& Key) { return Key.GetPairedAxisKey(); }
};
