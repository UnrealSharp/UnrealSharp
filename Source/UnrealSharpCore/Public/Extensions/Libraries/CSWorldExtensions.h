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

UENUM(BlueprintType)
enum ECSWorldType : int
{
	/** An untyped world, in most cases this will be the vestigial worlds of streamed in sub-levels */
	None = EWorldType::None,

	/** The game world */
	Game = EWorldType::Game,

	/** A world being edited in the editor */
	Editor = EWorldType::Editor,

	/** A Play In Editor world */
	PIE = EWorldType::PIE,

	/** A preview world for an editor tool */
	EditorPreview = EWorldType::EditorPreview,

	/** A preview world for a game */
	GamePreview = EWorldType::GamePreview,

	/** A minimal RPC world for a game */
	GameRPC = EWorldType::GameRPC,

	/** An editor world that was loaded but not currently being edited in the level editor */
	Inactive = EWorldType::Inactive,
};

static_assert(sizeof(ECSWorldType) == sizeof(EWorldType::Type), "ECSWorldType size does not match EWorldType size");

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
	
	UFUNCTION(meta = (ScriptMethod))
	static void SeamlessTravel(const UObject* WorldContextObject, const FString& URL, bool bAbsolute = false);
	
	UFUNCTION(meta = (ScriptMethod))
	static ECSWorldType GetWorldType(const UObject* WorldContextObject);
	
private:
	static AActor* SpawnActor_Internal(const UObject* WorldContextObject, const TSubclassOf<AActor>& Class, const FTransform& Transform, const FCSSpawnActorParameters& SpawnParameters, bool bDeferConstruction);
};

