// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "UObject/Object.h"
#include "FOptionalPropertyExporter.generated.h"

/**
 * 
 */
UCLASS()
class UNREALSHARPCORE_API UFOptionalPropertyExporter : public UObject
{
	GENERATED_BODY()
	
public:

	UNREALSHARP_FUNCTION()
	static bool IsSet(FOptionalProperty* OptionalProperty, const void* ScriptValue);

	UNREALSHARP_FUNCTION()
	static void* MarkSetAndGetInitializedValuePointerToReplace(FOptionalProperty* OptionalProperty, void* Data);

	UNREALSHARP_FUNCTION()
	static void MarkUnset(FOptionalProperty* OptionalProperty, void* Data);

	UNREALSHARP_FUNCTION()
	static const void* GetValuePointerForRead(FOptionalProperty* OptionalProperty, const void* Data);

	UNREALSHARP_FUNCTION()
	static void* GetValuePointerForReplace(FOptionalProperty* OptionalProperty, void* Data);

	UNREALSHARP_FUNCTION()
	static const void* GetValuePointerForReadIfSet(FOptionalProperty* OptionalProperty, const void* Data);

	UNREALSHARP_FUNCTION()
	static void* GetValuePointerForReplaceIfSet(FOptionalProperty* OptionalProperty, void* Data);
	
	UNREALSHARP_FUNCTION()
	static void* GetValuePointerForReadOrReplace(FOptionalProperty* OptionalProperty, void* Data);

	UNREALSHARP_FUNCTION()
	static void* GetValuePointerForReadOrReplaceIfSet(FOptionalProperty* OptionalProperty, void* Data);

	UNREALSHARP_FUNCTION()
	static int32 CalcSize(FOptionalProperty* OptionalProperty);
};
