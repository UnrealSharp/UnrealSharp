// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "FCSTypeRegistryExporter.generated.h"

UCLASS()
class UFCSTypeRegistryExporter : public UObject
{
	GENERATED_BODY()

public:

	UNREALSHARP_FUNCTION()
	static void RegisterClassToFilePath(const UTF16CHAR* ClassName, const UTF16CHAR* FilePath);
	
};
