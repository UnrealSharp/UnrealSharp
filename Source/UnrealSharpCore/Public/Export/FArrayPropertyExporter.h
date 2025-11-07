// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "FArrayPropertyExporter.generated.h"

UCLASS()
class UNREALSHARPCORE_API UFArrayPropertyExporter : public UObject
{
	GENERATED_BODY()

public:

	UNREALSHARP_FUNCTION()
	static void InitializeArray(FArrayProperty* ArrayProperty, const void* ScriptArray, int Length);

	UNREALSHARP_FUNCTION()
	static void EmptyArray(FArrayProperty* ArrayProperty, const void* ScriptArray);

	UNREALSHARP_FUNCTION()
	static void AddToArray(FArrayProperty* ArrayProperty, const void* ScriptArray);

	UNREALSHARP_FUNCTION()
	static void InsertInArray(FArrayProperty* ArrayProperty, const void* ScriptArray, int index);
	
	UNREALSHARP_FUNCTION()
	static void RemoveFromArray(FArrayProperty* ArrayProperty, const void* ScriptArray, int index);

	UNREALSHARP_FUNCTION()
	static void ResizeArray(FArrayProperty* ArrayProperty, const void* ScriptArray, int Length);

	UNREALSHARP_FUNCTION()
	static void SwapValues(FArrayProperty* ArrayProperty, const void* ScriptArray, int indexA, int indexB);
	
};
