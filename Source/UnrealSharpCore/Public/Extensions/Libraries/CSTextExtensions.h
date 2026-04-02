#pragma once

#include "CoreMinimal.h"
#include "UObject/Object.h"
#include "CSTextExtensions.generated.h"

UCLASS(meta = (InternalType))
class UCSTextExtensions : public UBlueprintFunctionLibrary
{
	GENERATED_BODY()
public:
	UFUNCTION(meta=(ScriptMethod))
	static FText Format(const FText& InPattern, const TArray<FText>& InArguments);
};
