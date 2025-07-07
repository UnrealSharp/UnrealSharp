// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "CSManagedGCHandle.h"
#include "UObject/Object.h"
#include "ManagedHandleExporter.generated.h"

/**
 * 
 */
UCLASS()
class UNREALSHARPCORE_API UManagedHandleExporter : public UObject {
    GENERATED_BODY()

public:
    UNREALSHARP_FUNCTION()
    static void StoreManagedHandle(FGCHandleIntPtr Handle, FSharedGCHandle& Destination);

    UNREALSHARP_FUNCTION()
    static FGCHandleIntPtr LoadManagedHandle(const FSharedGCHandle& Source);
};
