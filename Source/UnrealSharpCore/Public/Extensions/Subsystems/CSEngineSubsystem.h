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
	virtual void Initialize(FSubsystemCollectionBase& Collection) override;
	virtual void Deinitialize() override;
	virtual bool ShouldCreateSubsystem(UObject* Outer) const override;
	// End

	// FTickableGameObject Begin
	virtual void Tick(float DeltaTime) override;
	virtual ETickableTickType GetTickableTickType() const override;
	virtual bool IsTickable() const override;
	virtual TStatId GetStatId() const override;
	// End

public:

	UPROPERTY(EditAnywhere)
	bool bIsTickable;

	UFUNCTION(BlueprintCallable)
	void SetIsTickable(bool bInIsTickable)
	{
		bIsTickable = bInIsTickable;
	}

protected:

	UFUNCTION(BlueprintNativeEvent, meta = (ScriptName = "ShouldCreateSubsystem"))
	bool K2_ShouldCreateSubsystem(UObject* SubsystemOuter) const;

	UFUNCTION(BlueprintImplementableEvent, meta = (ScriptName = "Initialize"))
	void K2_Initialize(FSubsystemCollectionBaseRef Collection);

	UFUNCTION(BlueprintImplementableEvent, meta = (ScriptName = "Deinitialize"))
	void K2_Deinitialize();

	UFUNCTION(BlueprintImplementableEvent, meta = (ScriptName = "Tick"))
	void K2_Tick(float DeltaTime);

};
