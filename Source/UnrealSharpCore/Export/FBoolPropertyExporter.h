// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "FBoolPropertyExporter.generated.h"

UCLASS()
class UNREALSHARPCORE_API UFBoolPropertyExporter : public UObject
{
	GENERATED_BODY()

public:
	UNREALSHARP_FUNCTION()
	static bool GetBitfieldValueFromProperty(uint8* NativeBuffer, FProperty* Property, int32 Offset);

	UNREALSHARP_FUNCTION()
	static void SetBitfieldValueForProperty(uint8* NativeObject, FProperty* Property, int32 Offset, bool Value);
	
};
