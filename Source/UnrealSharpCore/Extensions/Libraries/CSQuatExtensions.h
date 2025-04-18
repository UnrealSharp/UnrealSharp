#pragma once

#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include "CSQuatExtensions.generated.h"

UCLASS(meta = (Internal))
class UCSQuatExtensions : public UBlueprintFunctionLibrary
{
	GENERATED_BODY()
public:
	UFUNCTION(meta=(ScriptMethod))
	static void ToQuaternion(FQuat& Quaternion, const FRotator& Rotator);

	UFUNCTION(meta=(ScriptMethod))
	static void ToRotator(FRotator& Rotator, const FQuat& Quaternion);
};
