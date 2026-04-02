#pragma once

#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include "CSPlayerControllerExtensions.generated.h"

UCLASS(meta = (InternalType))
class UCSPlayerControllerExtensions : public UBlueprintFunctionLibrary
{
	GENERATED_BODY()
public:
	UFUNCTION(meta = (ScriptMethod))
	static ULocalPlayer* GetLocalPlayer(APlayerController* PlayerController);
};
