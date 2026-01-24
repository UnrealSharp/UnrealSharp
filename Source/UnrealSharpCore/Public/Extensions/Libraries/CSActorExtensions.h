#pragma once

#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include "CSActorExtensions.generated.h"

struct FComponentOverrideRecord;
struct FComponentKey;

UCLASS(meta = (InternalType))
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

	UFUNCTION(meta=(ScriptMethod))
	static UActorComponent* GetComponentTemplate(const AActor* Actor, FName ComponentName);

	UFUNCTION(meta=(ScriptMethod))
	static FBox GetComponentsBoundingBox(const AActor* Actor, bool bNonColliding = false, bool bIncludeFromChildActors = false);
	
	UFUNCTION(meta=(ScriptMethod))
	static bool GetReplicates(AActor* Actor);
	
	UFUNCTION(meta=(ScriptMethod))
	static void SetReplicates(AActor* Actor, bool bReplicates);
	
	UFUNCTION(meta=(ScriptMethod))
	static void MarkAsNetworkAddressable(AActor* Actor);

public:
	static void CreateNewRecord(const UInheritableComponentHandler* InheritableComponentHandler, const FComponentKey& Key, FComponentOverrideRecord* NewRecord);
	static UActorComponent* FindBestArchetype(UObject* Outer, FComponentKey Key, FName TemplateName = NAME_None);
};
