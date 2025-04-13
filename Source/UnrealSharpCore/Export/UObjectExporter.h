﻿// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "UObjectExporter.generated.h"

UCLASS()
class UNREALSHARPCORE_API UUObjectExporter : public UObject
{
	GENERATED_BODY()

public:

	UNREALSHARP_FUNCTION()
	static void* CreateNewObject(UObject* Outer, UClass* Class, UObject* Template);

	UNREALSHARP_FUNCTION()
	static TSharedPtr<int> TestFunc() { return nullptr; }
	
	UNREALSHARP_FUNCTION()
	static void* GetTransientPackage();

	UNREALSHARP_FUNCTION()
	static void NativeGetName(UObject* Object, FName& OutName);

	UNREALSHARP_FUNCTION()
	static void InvokeNativeFunction(UObject* NativeObject, UFunction* NativeFunction, uint8* Params);
	
	UNREALSHARP_FUNCTION()
	static void InvokeNativeStaticFunction(const UClass* NativeClass, UFunction* NativeFunction, uint8* Params);

	UNREALSHARP_FUNCTION()
	static bool NativeIsValid(UObject* Object);

	UNREALSHARP_FUNCTION()
	static void* GetWorld_Internal(UObject* Object);

	UNREALSHARP_FUNCTION()
	static uint32 GetUniqueID(UObject* Object);

};
