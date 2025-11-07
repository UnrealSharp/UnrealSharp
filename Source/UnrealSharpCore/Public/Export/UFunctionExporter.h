// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "UFunctionExporter.generated.h"

UCLASS()
class UNREALSHARPCORE_API UUFunctionExporter : public UObject
{
	GENERATED_BODY()

public:

	UNREALSHARP_FUNCTION()
	static uint16 GetNativeFunctionParamsSize(const UFunction* NativeFunction);

	UNREALSHARP_FUNCTION()
	static UFunction* CreateNativeFunctionCustomStructSpecialization(UFunction* NativeFunction, FProperty** CustomStructParams, UScriptStruct** CustomStructs);

	UNREALSHARP_FUNCTION()
	static void InitializeFunctionParams(UFunction* NativeFunction, void* Params);

	UNREALSHARP_FUNCTION()
	static bool HasBlueprintEventBeenImplemented(const UFunction* NativeFunction);

};
