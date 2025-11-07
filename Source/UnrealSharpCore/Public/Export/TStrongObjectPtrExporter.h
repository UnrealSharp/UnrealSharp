// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "UObject/Object.h"
#include "TStrongObjectPtrExporter.generated.h"

/**
 * 
 */
UCLASS()
class UNREALSHARPCORE_API UTStrongObjectPtrExporter : public UObject
{
    GENERATED_BODY()

public:
    UNREALSHARP_FUNCTION()
    static void ConstructStrongObjectPtr(TStrongObjectPtr<UObject>* Ptr, UObject* Object);
    
    UNREALSHARP_FUNCTION()
    static void DestroyStrongObjectPtr(TStrongObjectPtr<UObject>* Ptr);
};
