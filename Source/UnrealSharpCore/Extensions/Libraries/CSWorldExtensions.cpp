#include "CSWorldExtensions.h"
#include "UnrealSharpCore.h"

AActor* UCSWorldExtensions::SpawnActor(const UObject* WorldContextObject, const TSubclassOf<AActor>& Class, const FTransform& Transform, const FCSSpawnActorParameters& InSpawnParameters)
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

	FActorSpawnParameters SpawnParameters;
	SpawnParameters.Instigator = InSpawnParameters.Instigator;
	SpawnParameters.Owner = InSpawnParameters.Owner;
	SpawnParameters.Template = InSpawnParameters.Template;
	SpawnParameters.bDeferConstruction = InSpawnParameters.DeferConstruction;
	SpawnParameters.SpawnCollisionHandlingOverride = InSpawnParameters.SpawnMethod;
	
	return World->SpawnActor(Class, &Transform, SpawnParameters);
}

void UCSWorldExtensions::FinishSpawning(AActor* Actor, const FTransform& UserTransform, bool bIsDefaultTransform)
{
	Actor->FinishSpawning(UserTransform, bIsDefaultTransform);
}