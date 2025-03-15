// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "FPropertyExporter.generated.h"

UCLASS()
class UNREALSHARPCORE_API UFPropertyExporter : public UObject
{
	GENERATED_BODY()

public:

	UNREALSHARP_FUNCTION()
	static FProperty* GetNativePropertyFromName(UStruct* Struct, const char* PropertyName);

	UNREALSHARP_FUNCTION()
	static int32 GetPropertyOffsetFromName(UStruct* InStruct, const char* InPropertyName);

	UNREALSHARP_FUNCTION()
	static int32 GetPropertyArrayDimFromName(UStruct* InStruct, const char* PropertyName);

	UNREALSHARP_FUNCTION()
	static int32 GetPropertyOffset(FProperty* Property);

	UNREALSHARP_FUNCTION()
	static int32 GetSize(FProperty* Property);
	
	UNREALSHARP_FUNCTION()
	static int32 GetArrayDim(FProperty* Property);

	UNREALSHARP_FUNCTION()
	static void DestroyValue(FProperty* Property, void* Value);

	UNREALSHARP_FUNCTION()
	static void DestroyValue_InContainer(FProperty* Property, void* Value);

	UNREALSHARP_FUNCTION()
	static void InitializeValue(FProperty* Property, void* Value);

	UNREALSHARP_FUNCTION()
	static bool Identical(const FProperty* Property, void* ValueA, void* ValueB);

	UNREALSHARP_FUNCTION()
	static void GetInnerFields(FProperty* SetProperty, TArray<FField*>* OutFields);

	UNREALSHARP_FUNCTION()
	static uint32 GetValueTypeHash(FProperty* Property, void* Source);

	UNREALSHARP_FUNCTION()
	static bool HasAnyPropertyFlags(FProperty* Property, EPropertyFlags FlagsToCheck);

	UNREALSHARP_FUNCTION()
	static bool HasAllPropertyFlags(FProperty* Property, EPropertyFlags FlagsToCheck);

	UNREALSHARP_FUNCTION()
	static void CopySingleValue(FProperty* Property, void* Dest, void* Src);

	UNREALSHARP_FUNCTION()
	static void GetValue_InContainer(FProperty* Property, void* Container, void* OutValue);

	UNREALSHARP_FUNCTION()
	static void SetValue_InContainer(FProperty* Property, void* Container, void* Value);

	UNREALSHARP_FUNCTION()
	static uint8 GetBoolPropertyFieldMaskFromName(UStruct* InStruct, const char* InPropertyName);
};
