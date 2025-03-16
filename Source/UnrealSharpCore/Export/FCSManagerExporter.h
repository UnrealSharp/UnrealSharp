// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "FCSManagerExporter.generated.h"

UCLASS()
class UNREALSHARPCORE_API UFCSManagerExporter : public UObject
{
	GENERATED_BODY()

public:

	UNREALSHARP_FUNCTION()
	static void* FindManagedObject(UObject* Object);

	UNREALSHARP_FUNCTION()
	static void* GetCurrentWorldContext();
	
	UNREALSHARP_FUNCTION()
	static void* GetCurrentWorldPtr();
	
};
