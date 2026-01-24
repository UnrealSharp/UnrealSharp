#pragma once

#include "CSWorldExtensions.generated.h"

USTRUCT()
struct FCSSpawnActorParameters
{
	GENERATED_BODY()
	
	UPROPERTY()
	TObjectPtr<AActor> Owner = nullptr;

	UPROPERTY()
	TObjectPtr<APawn> Instigator = nullptr;

	UPROPERTY()
	TObjectPtr<AActor> Template = nullptr;
	
	UPROPERTY()
	FName Name;

	UPROPERTY()
	ESpawnActorCollisionHandlingMethod SpawnMethod = ESpawnActorCollisionHandlingMethod::Undefined;
};

UCLASS(meta = (InternalType))
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
	
	UFUNCTION(meta = (ScriptMethod))
	static void ServerTravel(const UObject* WorldContextObject, const FString& URL, bool bAbsolute = false, bool bShouldSkipGameNotify = false);
	
private:
	static AActor* SpawnActor_Internal(const UObject* WorldContextObject, const TSubclassOf<AActor>& Class, const FTransform& Transform, const FCSSpawnActorParameters& SpawnParameters, bool bDeferConstruction);
};

