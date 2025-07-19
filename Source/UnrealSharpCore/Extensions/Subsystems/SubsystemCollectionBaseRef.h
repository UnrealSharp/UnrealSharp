// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "SubsystemCollectionBaseRef.generated.h"

USTRUCT(BlueprintType)
struct FSubsystemCollectionBaseRef
{
    GENERATED_BODY()

    FSubsystemCollectionBaseRef() = default;
    explicit(false) FSubsystemCollectionBaseRef(FSubsystemCollectionBase &Base) : Base(&Base) {}

private:
    FSubsystemCollectionBase *Base;
};
