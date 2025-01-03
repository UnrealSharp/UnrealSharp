#pragma once

#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include "CSActorComponentExtensions.generated.h"

UCLASS(meta = (Internal))
class UCSActorComponentExtensions : public UBlueprintFunctionLibrary
{
	GENERATED_BODY()
public:
	UFUNCTION(meta=(ScriptMethod))
	static void AddReplicatedSubObject(UActorComponent* ActorComponent, UObject* SubObject, ELifetimeCondition NetCondition);
	
	UFUNCTION(meta=(ScriptMethod))
	static void RemoveReplicatedSubObject(UActorComponent* ActorComponent, UObject* SubObject);
	
	UFUNCTION(meta=(ScriptMethod))
	static bool IsReplicatedSubObjectRegistered(UActorComponent* ActorComponent, UObject* SubObject);
};
