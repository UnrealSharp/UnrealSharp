// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "UObject/Object.h"
#include "FFieldPathExporter.generated.h"

UCLASS()
class UNREALSHARPCORE_API UFFieldPathExporter : public UObject
{
	GENERATED_BODY()

public:
	UNREALSHARP_FUNCTION()
	static bool IsValid(const TFieldPath<FField>& FieldPath);

	UNREALSHARP_FUNCTION()
	static bool IsStale(const FFieldPath& FieldPath);

	UNREALSHARP_FUNCTION()
	static void FieldPathToString(const FFieldPath& FieldPath, FString* OutString);

	UNREALSHARP_FUNCTION()
	static bool FieldPathsEqual(const FFieldPath& A, const FFieldPath& B);

	UNREALSHARP_FUNCTION()
	static int32 GetFieldPathHashCode(const FFieldPath& FieldPath);
};
