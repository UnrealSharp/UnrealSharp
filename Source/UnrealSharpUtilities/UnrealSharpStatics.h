// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include "UnrealSharpStatics.generated.h"

UCLASS()
class UNREALSHARPUTILITIES_API UUnrealSharpStatics : public UBlueprintFunctionLibrary
{
	GENERATED_BODY()

public:

	static FString GetNamespace(const UObject* Object);
	static FString GetNamespace(FName PackageName);
	
	static FName GetModuleName(const UObject* Object);
	
};
