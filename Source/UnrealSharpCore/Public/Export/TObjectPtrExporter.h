// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "UObject/Object.h"
#include "TObjectPtrExporter.generated.h"

UCLASS()
class UNREALSHARPCORE_API UTObjectPtrExporter : public UObject
{
	GENERATED_BODY()
public:
	UNREALSHARP_FUNCTION()
	static void SetTObjectPtrPropertyValue(TObjectPtr<UObject>* Object, UObject* NewValue);
};
