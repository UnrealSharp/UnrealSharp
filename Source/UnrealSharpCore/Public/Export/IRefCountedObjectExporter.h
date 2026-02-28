// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "UObject/Object.h"
#include "IRefCountedObjectExporter.generated.h"

UCLASS()
class UIRefCountedObjectExporter : public UObject
{
	GENERATED_BODY()
public:
	UNREALSHARP_FUNCTION()
	static uint32 GetRefCount(const IRefCountedObject* Object);

	UNREALSHARP_FUNCTION()
	static FReturnedRefCountValue AddRef(const IRefCountedObject* Object);

	UNREALSHARP_FUNCTION()
	static uint32 Release(const IRefCountedObject* Object);
};
