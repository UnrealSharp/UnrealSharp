#include "CSWorldExtensions.h"
#include "UnrealSharpCore.h"
#include "GameFramework/Actor.h"

AActor* UCSWorldExtensions::SpawnActor(const UObject* WorldContextObject, const TSubclassOf<AActor>& Class, const FTransform& Transform, const FCSSpawnActorParameters& InSpawnParameters)
{
	return SpawnActor_Internal(WorldContextObject, Class, Transform, InSpawnParameters, false);
}

AActor* UCSWorldExtensions::SpawnActorDeferred(const UObject* WorldContextObject, const TSubclassOf<AActor>& Class, const FTransform& Transform, const FCSSpawnActorParameters& SpawnParameters)
{
	return SpawnActor_Internal(WorldContextObject, Class, Transform, SpawnParameters, true);
}

void UCSWorldExtensions::ExecuteConstruction(AActor* Actor, const FTransform& Transform)
{
	Actor->ExecuteConstruction(Transform, nullptr, nullptr, true);
}

void UCSWorldExtensions::PostActorConstruction(AActor* Actor)
{
	Actor->PostActorConstruction();
	Actor->PostLoad();
}

AActor* UCSWorldExtensions::SpawnActor_Internal(const UObject* WorldContextObject, const TSubclassOf<AActor>& Class, const FTransform& Transform, const FCSSpawnActorParameters& SpawnParameters, bool bDeferConstruction)
{
	if (!IsValid(WorldContextObject) || !IsValid(Class))
	{
		UE_LOG(LogUnrealSharp, Error, TEXT("Invalid world context object or class"));
		return nullptr;
	}

	UWorld* World = GEngine->GetWorldFromContextObject(WorldContextObject, EGetWorldErrorMode::LogAndReturnNull);

	if (!IsValid(World))
	{
		UE_LOG(LogUnrealSharp, Error, TEXT("Failed to get world from context object"));
		return nullptr;
	}

	FActorSpawnParameters SpawnParams;
	SpawnParams.Instigator = SpawnParameters.Instigator;
	SpawnParams.Owner = SpawnParameters.Owner;
	SpawnParams.Template = SpawnParameters.Template;
	SpawnParams.SpawnCollisionHandlingOverride = SpawnParameters.SpawnMethod;
	SpawnParams.bDeferConstruction = bDeferConstruction;
	
	return World->SpawnActor(Class, &Transform, SpawnParams);
}

