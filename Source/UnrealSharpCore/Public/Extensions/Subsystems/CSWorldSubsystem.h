#pragma once

#include "CoreMinimal.h"
#if ENGINE_MINOR_VERSION >= 5
#include "Streaming/StreamingWorldSubsystemInterface.h"
#endif
#include "SubsystemCollectionBaseRef.h"
#include "Subsystems/WorldSubsystem.h"
#include "CSWorldSubsystem.generated.h"

UENUM(BlueprintType)
enum class ECSWorldType : uint8
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


UCLASS(Blueprintable, BlueprintType, Abstract)
class UCSWorldSubsystem : public UTickableWorldSubsystem
#if ENGINE_MINOR_VERSION >= 5
	, public IStreamingWorldSubsystemInterface
#endif
{
	GENERATED_BODY()

	// USubsystem Begin
	virtual void Initialize(FSubsystemCollectionBase& Collection) override;
	virtual void Deinitialize() override;
	virtual bool ShouldCreateSubsystem(UObject* Outer) const override;
	virtual bool DoesSupportWorldType(const EWorldType::Type WorldType) const override;
	virtual void BeginDestroy() override;
	// End

	// UWorldSubsystem begin
	virtual void PostInitialize() override;
	virtual void OnWorldBeginPlay(UWorld& InWorld) override;
	virtual void OnWorldComponentsUpdated(UWorld& World) override;

#if ENGINE_MINOR_VERSION >= 5
	virtual void OnUpdateStreamingState() override
	{
        K2_UpdateStreamingState();
	}
#else
	virtual void UpdateStreamingState() override
	{
		Super::UpdateStreamingState();
		K2_UpdateStreamingState();
	}
#endif
	virtual TStatId GetStatId() const override;
	virtual void Tick(float DeltaTime) override;
	// End

	/** Returns true if Initialize has been called but Deinitialize has not */
	UFUNCTION(meta = (ScriptMethod))
	bool GetIsInitialized() const;

protected:

	UFUNCTION(BlueprintImplementableEvent, meta = (ScriptName = "PostInitialize"))
	void K2_PostInitialize();

	UFUNCTION(BlueprintImplementableEvent, meta = (ScriptName = "Tick"))
	void K2_Tick(float DeltaTime);

	UFUNCTION(BlueprintImplementableEvent, meta = (ScriptName = "OnWorldBeginPlay"))
	void K2_OnWorldBeginPlay();

	UFUNCTION(BlueprintImplementableEvent, meta = (ScriptName = "OnWorldComponentsUpdated"))
	void K2_OnWorldComponentsUpdated();

	UFUNCTION(BlueprintImplementableEvent, meta = (ScriptName = "UpdateStreamingState"))
	void K2_UpdateStreamingState();

	UFUNCTION(BlueprintNativeEvent, meta = (ScriptName = "ShouldCreateSubsystem"))
	bool K2_ShouldCreateSubsystem(UObject* SubsystemOuter) const;

    UFUNCTION(BlueprintNativeEvent, meta = (ScriptName = "DoesSupportWorldType"))
    bool K2_DoesSupportWorldType(const ECSWorldType WorldType) const;

	UFUNCTION(BlueprintImplementableEvent, meta = (ScriptName = "Initialize"))
	void K2_Initialize(FSubsystemCollectionBaseRef Collection);

	UFUNCTION(BlueprintImplementableEvent, meta = (ScriptName = "Deinitialize"))
	void K2_Deinitialize();

};
