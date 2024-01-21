// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "CSFunction.generated.h"

UCLASS()
class CSHARPFORUE_API UCSFunction : public UFunction
{
	GENERATED_BODY()

public:
	
	void SetManagedMethod(void* InManagedMethod);
	void* GetManagedMethod() const;

private:

	void* ManagedMethod;
	
};
