#pragma once

#include "CSWorldExtensions.generated.h"

USTRUCT()
struct FCSSpawnActorParameters
{
	GENERATED_BODY()
	
	UPROPERTY()
	AActor* Owner = nullptr;

	UPROPERTY()
	APawn* Instigator = nullptr;

	UPROPERTY()
	AActor* Template = nullptr;

	UPROPERTY()
	ESpawnActorCollisionHandlingMethod SpawnMethod = ESpawnActorCollisionHandlingMethod::Undefined;
};

UCLASS(meta = (Internal))
class UCSWorldExtensions : public UBlueprintFunctionLibrary
{
	GENERATED_BODY()
public:
	UFUNCTION(meta = (ScriptMethod))
	static AActor* SpawnActor(const UObject* WorldContextObject, const TSubclassOf<AActor>& Class, const FTransform& Transform, const FCSSpawnActorParameters& SpawnParameters);

	UFUNCTION(meta = (ScriptMethod))
	static AActor* SpawnActorDeferred(const UObject* WorldContextObject, const TSubclassOf<AActor>& Class, const FTransform& Transform, const FCSSpawnActorParameters& SpawnParameters);

	UFUNCTION(meta = (ScriptMethod))
	static void ExecuteConstruction(AActor* Actor, const FTransform& Transform);

	UFUNCTION(meta = (ScriptMethod))
	static void PostActorConstruction(AActor* Actor);

	UFUNCTION(meta = (ScriptMethod))
	static FURL WorldURL(const UObject* WorldContextObject);
private:
	static AActor* SpawnActor_Internal(const UObject* WorldContextObject, const TSubclassOf<AActor>& Class, const FTransform& Transform, const FCSSpawnActorParameters& SpawnParameters, bool bDeferConstruction);
};

