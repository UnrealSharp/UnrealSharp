#pragma once

#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include "CSSystemExtensions.generated.h"

UCLASS(meta = (Internal))
class UCSSystemExtensions : public UBlueprintFunctionLibrary
{
	GENERATED_BODY()
public:
	UFUNCTION(meta=(ScriptMethod))
	static void PrintStringInternal(UObject* WorldContextObject, const FString& InString, bool bPrintToScreen, bool bPrintToLog,
		FLinearColor TextColor,
		float Duration,
		const FName Key);
};
