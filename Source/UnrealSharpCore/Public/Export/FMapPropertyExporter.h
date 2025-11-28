// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "UObject/Object.h"
#include "FMapPropertyExporter.generated.h"

UCLASS()
class UFMapPropertyExporter : public UObject
{
	GENERATED_BODY()
public:
	UNREALSHARP_FUNCTION()
	static void* GetKey(FMapProperty* MapProperty);

	UNREALSHARP_FUNCTION()
	static void* GetValue(FMapProperty* MapProperty);
};
