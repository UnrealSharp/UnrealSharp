// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "SubsystemCollectionBaseRef.h"
#include "Subsystems/GameInstanceSubsystem.h"
#include "CSGameInstanceSubsystem.generated.h"

UCLASS(Blueprintable, BlueprintType, Abstract)
class UCSGameInstanceSubsystem : public UGameInstanceSubsystem, public FTickableGameObject
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
	void SetIsTickable(bool bInIsTickable);

protected:

	UFUNCTION(BlueprintCallable, meta = (ScriptName = "GetGameInstance"), DisplayName = "Get Game Instance")
	UGameInstance* K2_GetGameInstance() const;

	UFUNCTION(BlueprintNativeEvent, meta = (ScriptName = "ShouldCreateSubsystem"))
	bool K2_ShouldCreateSubsystem(UObject* SubsystemOuter) const;

	UFUNCTION(BlueprintImplementableEvent, meta = (ScriptName = "Initialize"))
	void K2_Initialize(FSubsystemCollectionBaseRef Collection);

	UFUNCTION(BlueprintImplementableEvent, meta = (ScriptName = "Deinitialize"))
	void K2_Deinitialize();

	UFUNCTION(BlueprintImplementableEvent, meta = (ScriptName = "Tick"))
	void K2_Tick(float DeltaTime);
};
