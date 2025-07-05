// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "UWorldExporter.generated.h"

UCLASS()
class UNREALSHARPCORE_API UUWorldExporter : public UObject
{
	GENERATED_BODY()

public:

	UNREALSHARP_FUNCTION()
	static void SetTimer(UObject* Object, FName FunctionName, float Rate, bool Loop, float InitialDelay, FTimerHandle* TimerHandle);

	UNREALSHARP_FUNCTION()
	static void InvalidateTimer(UObject* Object, FTimerHandle* TimerHandle);

	UNREALSHARP_FUNCTION()
	static void* GetWorldSubsystem(UClass* SubsystemClass, UObject* WorldContextObject);

	UNREALSHARP_FUNCTION()
	static void* GetNetMode(UObject* WorldContextObject);
};
