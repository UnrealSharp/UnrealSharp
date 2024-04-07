// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include "UnrealSharpStatics.generated.h"

#define UNREAL_SHARP_NAMESPACE TEXT("UnrealSharp")
#define UNREAL_SHARP_OBJECT TEXT("UnrealSharpObject")
#define UNREAL_SHARP_RUNTIME_NAMESPACE UNREAL_SHARP_NAMESPACE TEXT(".Runtime")
#define UNREAL_SHARP_ENGINE_NAMESPACE UNREAL_SHARP_NAMESPACE TEXT(".Engine")
#define UNREAL_SHARP_ATTRIBUTES_NAMESPACE UNREAL_SHARP_NAMESPACE TEXT(".Attributes")

UCLASS()
class UNREALSHARPUTILITIES_API UUnrealSharpStatics : public UBlueprintFunctionLibrary
{
	GENERATED_BODY()

public:

	static FString GetNamespace(const UObject* Object);
	static FString GetNamespace(FName PackageName);

	static FName GetModuleName(const UObject* Object);
	
};
