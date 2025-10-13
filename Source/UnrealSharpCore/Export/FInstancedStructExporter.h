// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "UObject/Object.h"
#include "FInstancedStructExporter.generated.h"

struct FInstancedStruct;
/**
 * 
 */
UCLASS()
class UNREALSHARPCORE_API UFInstancedStructExporter : public UObject
{
	GENERATED_BODY()

public:
	UNREALSHARP_FUNCTION()
	static const UScriptStruct* GetNativeStruct(const FInstancedStruct& Struct);

	UNREALSHARP_FUNCTION()
	static void NativeInit(FInstancedStruct& Struct);

	UNREALSHARP_FUNCTION()
	static void NativeCopy(FInstancedStruct& Dest, const FInstancedStruct& Src);

	UNREALSHARP_FUNCTION()
	static void NativeDestroy(FInstancedStruct& Struct);

	UNREALSHARP_FUNCTION()
	static void InitializeAs(FInstancedStruct& Struct, const UScriptStruct* ScriptStruct, const uint8* StructData);

	UNREALSHARP_FUNCTION()
	static const uint8* GetMemory(const FInstancedStruct& Struct);
};
