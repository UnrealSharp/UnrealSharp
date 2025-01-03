#pragma once

#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include "CSActorExtensions.generated.h"

UCLASS(meta = (Internal))
class UCSActorExtensions : public UBlueprintFunctionLibrary
{
	GENERATED_BODY()
public:
	UFUNCTION(meta=(ScriptMethod))
	static void AddReplicatedSubObject(AActor* Actor, UObject* SubObject, ELifetimeCondition NetCondition);
	
	UFUNCTION(meta=(ScriptMethod))
	static void RemoveReplicatedSubObject(AActor* Actor, UObject* SubObject);
	
	UFUNCTION(meta=(ScriptMethod))
	static bool IsReplicatedSubObjectRegistered(AActor* Actor, UObject* SubObject);
};
