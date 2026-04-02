#include "Extensions/Libraries/CSWorldExtensions.h"
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

FURL UCSWorldExtensions::WorldURL(const UObject* WorldContextObject)
{
	UWorld* World = GEngine->GetWorldFromContextObject(WorldContextObject, EGetWorldErrorMode::LogAndReturnNull);
	return World->URL;
}

void UCSWorldExtensions::ServerTravel(const UObject* WorldContextObject, const FString& URL, bool bAbsolute, bool bShouldSkipGameNotify)
{
	UWorld* World = GEngine->GetWorldFromContextObject(WorldContextObject, EGetWorldErrorMode::LogAndReturnNull);
	World->ServerTravel(URL, bAbsolute, bShouldSkipGameNotify);
}

void UCSWorldExtensions::SeamlessTravel(const UObject* WorldContextObject, const FString& URL, bool bAbsolute)
{
	UWorld* World = GEngine->GetWorldFromContextObject(WorldContextObject, EGetWorldErrorMode::LogAndReturnNull);
	World->SeamlessTravel(URL, bAbsolute);
}

ECSWorldType UCSWorldExtensions::GetWorldType(const UObject* WorldContextObject)
{
	UWorld* World = GEngine->GetWorldFromContextObject(WorldContextObject, EGetWorldErrorMode::LogAndReturnNull);
	return static_cast<ECSWorldType>(World->WorldType.GetValue());
}

AActor* UCSWorldExtensions::SpawnActor_Internal(const UObject* WorldContextObject, const TSubclassOf<AActor>& Class, const FTransform& Transform, const FCSSpawnActorParameters& SpawnParameters, bool bDeferConstruction)
{
	if (!IsValid(WorldContextObject) || !IsValid(Class))
	{
		UE_LOG(LogUnrealSharp, Error, TEXT("Invalid world context object or class"));
		return nullptr;
	}
	
	FActorSpawnParameters SpawnParams;
	SpawnParams.Instigator = SpawnParameters.Instigator;
	SpawnParams.Owner = SpawnParameters.Owner;
	SpawnParams.Template = SpawnParameters.Template;
	SpawnParams.SpawnCollisionHandlingOverride = SpawnParameters.SpawnMethod;
	SpawnParams.bDeferConstruction = bDeferConstruction;
	SpawnParams.Name = SpawnParameters.Name;
	
	UWorld* World = GEngine->GetWorldFromContextObject(WorldContextObject, EGetWorldErrorMode::LogAndReturnNull);
	return World->SpawnActor(Class, &Transform, SpawnParams);
}

