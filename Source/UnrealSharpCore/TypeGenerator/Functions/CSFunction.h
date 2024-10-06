// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "CSFunction.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSFunctionBase : public UFunction
{
	GENERATED_BODY()

public:
	
	void SetManagedMethod(void* InManagedMethod);

protected:
	
	static bool InvokeManagedEvent(UObject* ObjectToInvokeOn, FFrame& Stack, const UCSFunctionBase* Function, uint8* ArgumentBuffer, RESULT_DECL);
	
	void* ManagedMethod;
	
};
