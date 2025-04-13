// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "UAssetManagerExporter.generated.h"

UCLASS()
class UNREALSHARPCORE_API UUAssetManagerExporter : public UObject
{
	GENERATED_BODY()

public:

	UNREALSHARP_FUNCTION()
	static void* GetAssetManager();
	
};
