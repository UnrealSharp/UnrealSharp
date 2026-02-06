// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include "CSUserWidgetExtensions.generated.h"

/**
 * UCSUserWidgetExtensions
 *
 * Purpose:
 *   - Exposes UUserWidget helpers to managed code via UnrealSharp script methods.
 *
 * Responsibilities:
 *   - Provide owning player/state accessors for user widgets.
 *   - Create user widgets from world context objects or parent widgets.
 *   - Enumerate widget tree children for managed callers.
 *
 * Dependencies:
 *   - UWidgetBlueprintLibrary for widget creation.
 *   - UWidgetTree for widget hierarchy traversal.
 */
UCLASS(meta = (InternalType))
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

	UFUNCTION(meta=(ScriptMethod, UserWidgetClass = "/Script/UMG.UserWidget", DeterminesOutputType = "UserWidgetClass"))
	static UUserWidget* CreateWidget(UObject* WorldContextObject, const TSubclassOf<UUserWidget>& UserWidgetClass, APlayerController* OwningController);

	UFUNCTION(meta=(ScriptMethod, UserWidgetClass = "/Script/UMG.UserWidget", DeterminesOutputType = "UserWidgetClass"))
	static UUserWidget* CreateWidget_WithWidget(UUserWidget* OwningWidget, const TSubclassOf<UUserWidget>& UserWidgetClass);

	UFUNCTION(meta=(ScriptMethod))
	static TArray<UWidget*> GetAllWidgets(UUserWidget* UserWidget);
};
