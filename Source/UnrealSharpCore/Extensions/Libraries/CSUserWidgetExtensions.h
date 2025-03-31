// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include "CSUserWidgetExtensions.generated.h"

UCLASS(meta = (Internal))
class UCSUserWidgetExtensions : public UBlueprintFunctionLibrary
{
	GENERATED_BODY()
public:
	UFUNCTION(meta=(ScriptMethod))
	static APlayerController* GetOwningPlayerController(UUserWidget* UserWidget);

	UFUNCTION(meta=(ScriptMethod))
	static void SetOwningPlayerController(UUserWidget* UserWidget, APlayerController* PlayerController);

	UFUNCTION(meta=(ScriptMethod))
	static APlayerState* GetOwningPlayerState(UUserWidget* UserWidget);

	UFUNCTION(meta=(ScriptMethod))
	static ULocalPlayer* GetOwningLocalPlayer(UUserWidget* UserWidget);
};
