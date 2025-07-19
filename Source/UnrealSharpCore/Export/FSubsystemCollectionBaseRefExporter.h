// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "UObject/Object.h"
#include "FSubsystemCollectionBaseRefExporter.generated.h"

/**
 *
 */
UCLASS()
class UNREALSHARPCORE_API UFSubsystemCollectionBaseRefExporter : public UObject
{
    GENERATED_BODY()

public:
    UNREALSHARP_FUNCTION()
    static USubsystem* InitializeDependency(FSubsystemCollectionBase* Collection, UClass* SubsystemClass);
};
