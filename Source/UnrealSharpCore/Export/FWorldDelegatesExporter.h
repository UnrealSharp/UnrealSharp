// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "FWorldDelegatesExporter.generated.h"

using FWorldCleanupEventDelegate = void(*)(UWorld*, bool, bool);

UCLASS()
class UNREALSHARPCORE_API UFWorldDelegatesExporter : public UObject
{
	GENERATED_BODY()
public:

	UNREALSHARP_FUNCTION()
	static void BindOnWorldCleanup(FWorldCleanupEventDelegate Delegate, FDelegateHandle* Handle);

	UNREALSHARP_FUNCTION()
	static void UnbindOnWorldCleanup(FDelegateHandle Handle);
	
};
