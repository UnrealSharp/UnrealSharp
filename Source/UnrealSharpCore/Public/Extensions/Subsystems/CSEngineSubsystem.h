// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "SubsystemCollectionBaseRef.h"
#include "Subsystems/EngineSubsystem.h"
#include "CSEngineSubsystem.generated.h"

UCLASS(Blueprintable, BlueprintType, Abstract)
class UCSEngineSubsystem : public UEngineSubsystem, public FTickableGameObject
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

	// FTickableGameObject Begin

	virtual void Tick(float DeltaTime) override
	{
		K2_Tick(DeltaTime);
	}

	virtual ETickableTickType GetTickableTickType() const override
	{
		return ETickableTickType::Conditional;
	}

	virtual bool IsTickable() const override
	{
		return bIsTickable;
	}

	virtual TStatId GetStatId() const override
	{
		RETURN_QUICK_DECLARE_CYCLE_STAT(UCSEngineSubsystem, STATGROUP_Tickables);
	}

	// End

public:

	UPROPERTY(EditAnywhere, Category = "Managed Subsystems")
	bool bIsTickable;

	UFUNCTION(BlueprintCallable, Category = "Managed Subsystems")
	void SetIsTickable(bool bInIsTickable)
	{
		bIsTickable = bInIsTickable;
	}

protected:

	UFUNCTION(BlueprintNativeEvent, meta = (ScriptName = "ShouldCreateSubsystem"), Category = "Managed Subsystems")
	bool K2_ShouldCreateSubsystem() const;

	UFUNCTION(BlueprintImplementableEvent, meta = (ScriptName = "Initialize"), Category = "Managed Subsystems")
	void K2_Initialize(FSubsystemCollectionBaseRef Collection);

	UFUNCTION(BlueprintImplementableEvent, meta = (ScriptName = "Deinitialize"), Category = "Managed Subsystems")
	void K2_Deinitialize();

	UFUNCTION(BlueprintImplementableEvent, meta = (ScriptName = "Tick"), Category = "Managed Subsystems")
	void K2_Tick(float DeltaTime);

};
