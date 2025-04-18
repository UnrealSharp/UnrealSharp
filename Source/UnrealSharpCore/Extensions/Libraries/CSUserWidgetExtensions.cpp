// Fill out your copyright notice in the Description page of Project Settings.

#include "CSUserWidgetExtensions.h"
#include "GameFramework/PlayerState.h"
#include "Blueprint/UserWidget.h"
#include "Blueprint/WidgetBlueprintLibrary.h"

APlayerController* UCSUserWidgetExtensions::GetOwningPlayerController(UUserWidget* UserWidget)
{
	if (!IsValid(UserWidget))
	{
		return nullptr;
	}

	return UserWidget->GetOwningPlayer();
}

void UCSUserWidgetExtensions::SetOwningPlayerController(UUserWidget* UserWidget, APlayerController* PlayerController)
{
	if (!IsValid(UserWidget))
	{
		return;
	}

	UserWidget->SetOwningPlayer(PlayerController);
}

APlayerState* UCSUserWidgetExtensions::GetOwningPlayerState(UUserWidget* UserWidget)
{
	if (!IsValid(UserWidget))
	{
		return nullptr;
	}
	
	return UserWidget->GetOwningPlayerState<APlayerState>();
}

ULocalPlayer* UCSUserWidgetExtensions::GetOwningLocalPlayer(UUserWidget* UserWidget)
{
	if (!IsValid(UserWidget))
	{
		return nullptr;
	}

	return UserWidget->GetOwningLocalPlayer();
}

UUserWidget* UCSUserWidgetExtensions::CreateWidget(UObject* WorldContextObject, const TSubclassOf<UUserWidget>& UserWidgetClass, APlayerController* OwningController)
{
	UUserWidget* UserWidget = UWidgetBlueprintLibrary::Create(WorldContextObject, UserWidgetClass, OwningController);
	return UserWidget;
}
