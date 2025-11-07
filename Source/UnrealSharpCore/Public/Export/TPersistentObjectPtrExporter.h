// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "TPersistentObjectPtrExporter.generated.h"

UCLASS()
class UNREALSHARPCORE_API UTPersistentObjectPtrExporter : public UObject
{
	GENERATED_BODY()

public:

	UNREALSHARP_FUNCTION()
	static void FromObject(TPersistentObjectPtr<FSoftObjectPath>* Path, UObject* Object);

	UNREALSHARP_FUNCTION()
	static void FromSoftObjectPath(TPersistentObjectPtr<FSoftObjectPath>* Path, const FSoftObjectPath* SoftObjectPath);

	UNREALSHARP_FUNCTION()
	static void* Get(TPersistentObjectPtr<FSoftObjectPath>* Path);

	UNREALSHARP_FUNCTION()
	static void* GetNativePointer(TPersistentObjectPtr<FSoftObjectPath>* Path);

	UNREALSHARP_FUNCTION()
	static void* GetUniqueID(TPersistentObjectPtr<FSoftObjectPath>* Path);
	
};
