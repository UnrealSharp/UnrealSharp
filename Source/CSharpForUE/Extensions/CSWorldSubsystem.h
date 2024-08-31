#pragma once

#include "CoreMinimal.h"
#include "Subsystems/WorldSubsystem.h"
#include "CSWorldSubsystem.generated.h"

UCLASS(Blueprintable, BlueprintType, Abstract)
class UCSWorldSubsystem : public UTickableWorldSubsystem
{
	GENERATED_BODY()

	// USubsystem Begin
	
	virtual void Initialize(FSubsystemCollectionBase& Collection) override
	{
		Super::Initialize(Collection);
		K2_Initialize();
	}
  
	virtual void Deinitialize() override
	{
		Super::Deinitialize();
		K2_Deinitialize();
	}
  
	virtual bool ShouldCreateSubsystem(UObject* Outer) const override
	{
		if (!Super::ShouldCreateSubsystem(Outer))
		{
			return false;
		}
  
		return K2_ShouldCreateSubsystem();
	}

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
	
	virtual void UpdateStreamingState() override
	{
		Super::UpdateStreamingState();
		K2_UpdateStreamingState();
	}

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
  
	UFUNCTION(BlueprintImplementableEvent, meta = (ScriptName = "Initialize"), Category = "Managed Subsystems")
	void K2_Initialize();
  
	UFUNCTION(BlueprintImplementableEvent, meta = (ScriptName = "Deinitialize"), Category = "Managed Subsystems")
	void K2_Deinitialize();
	
};
