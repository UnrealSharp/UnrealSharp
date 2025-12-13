#pragma once

#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include "CSObjectExtensions.generated.h"

UCLASS(meta = (InternalType))
class UCSObjectExtensions : public UBlueprintFunctionLibrary
{
	GENERATED_BODY()
public:
	UFUNCTION(meta=(ScriptMethod))
	static AWorldSettings* GetWorldSettings(const UObject* Object);

	UFUNCTION(meta=(ScriptMethod))
	static void MarkAsGarbage(UObject* Object);

	UFUNCTION(meta=(ScriptMethod))
	static bool IsTemplate(const UObject* Object);

	UFUNCTION(meta = (ScriptMethod))
	static UClass* K2_GetClass(const UObject* Object);

	UFUNCTION(meta = (ScriptMethod))
	static UObject* GetOuter(const UObject* Object);
};
