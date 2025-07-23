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

	virtual void Initialize(FSubsystemCollectionBase& Collection) override
	{
		Super::Initialize(Collection);
		K2_Initialize(Collection);
	}

	virtual void Deinitialize() override
	{
		if (IsInitialized())
		{
			Super::Deinitialize();
			K2_Deinitialize();
		}
	}

	virtual bool ShouldCreateSubsystem(UObject* Outer) const override
	{
		if (!Super::ShouldCreateSubsystem(Outer))
		{
			return false;
		}

		return K2_ShouldCreateSubsystem();
	}

    virtual bool DoesSupportWorldType(const EWorldType::Type WorldType) const override
	{
	    if (!Super::DoesSupportWorldType(WorldType))
	    {
	        return false;
	    }

	    return K2_DoesSupportWorldType(static_cast<ECSWorldType>(WorldType));
	}

	virtual void BeginDestroy() override;

	// End

	// UWorldSubsystem begin
	virtual void PostInitialize() override
	{
		Super::PostInitialize();
		K2_PostInitialize();
	}

	virtual void OnWorldBeginPlay(UWorld& InWorld) override
	{
		Super::OnWorldBeginPlay(InWorld);
		K2_OnWorldBeginPlay();
	}

	virtual void OnWorldComponentsUpdated(UWorld& World) override
	{
		Super::OnWorldComponentsUpdated(World);
		K2_OnWorldComponentsUpdated();
	}

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

	virtual TStatId GetStatId() const override
	{
		RETURN_QUICK_DECLARE_CYCLE_STAT(UCSWorldSubsystem, STATGROUP_Tickables);
	}

	virtual void Tick(float DeltaTime) override
	{
		Super::Tick(DeltaTime);
		K2_Tick(DeltaTime);
	}

	// End

	/** Returns true if Initialize has been called but Deinitialize has not */
	UFUNCTION(meta = (ScriptMethod))
	bool GetIsInitialized() const;

protected:

	UFUNCTION(BlueprintImplementableEvent, meta = (ScriptName = "PostInitialize"), Category = "Managed Subsystems")
	void K2_PostInitialize();

	UFUNCTION(BlueprintImplementableEvent, meta = (ScriptName = "Tick"), Category = "Managed Subsystems")
	void K2_Tick(float DeltaTime);

	UFUNCTION(BlueprintImplementableEvent, meta = (ScriptName = "OnWorldBeginPlay"), Category = "Managed Subsystems")
	void K2_OnWorldBeginPlay();

	UFUNCTION(BlueprintImplementableEvent, meta = (ScriptName = "OnWorldComponentsUpdated"), Category = "Managed Subsystems")
	void K2_OnWorldComponentsUpdated();

	UFUNCTION(BlueprintImplementableEvent, meta = (ScriptName = "UpdateStreamingState"), Category = "Managed Subsystems")
	void K2_UpdateStreamingState();

	UFUNCTION(BlueprintNativeEvent, meta = (ScriptName = "ShouldCreateSubsystem"), Category = "Managed Subsystems")
	bool K2_ShouldCreateSubsystem() const;

    UFUNCTION(BlueprintNativeEvent, meta = (ScriptName = "DoesSupportWorldType"), Category = "Managed Subsystems")
    bool K2_DoesSupportWorldType(const ECSWorldType WorldType) const;

	UFUNCTION(BlueprintImplementableEvent, meta = (ScriptName = "Initialize"), Category = "Managed Subsystems")
	void K2_Initialize(FSubsystemCollectionBaseRef Collection);

	UFUNCTION(BlueprintImplementableEvent, meta = (ScriptName = "Deinitialize"), Category = "Managed Subsystems")
	void K2_Deinitialize();

};
