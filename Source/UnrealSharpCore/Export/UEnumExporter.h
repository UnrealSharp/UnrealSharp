// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "CSManagedGCHandle.h"
#include "UObject/Object.h"
#include "UEnumExporter.generated.h"

/**
 * 
 */
UCLASS()
class UNREALSHARPCORE_API UUEnumExporter : public UObject
{
	GENERATED_BODY()

public:
	UNREALSHARP_FUNCTION()
	static FGCHandleIntPtr GetManagedEnumType(UEnum* ScriptEnum);
};
