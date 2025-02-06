// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "CSFunction.generated.h"

class UCSClass;
struct FGCHandle;

UCLASS()
class UCSFunctionBase : public UFunction
{
	GENERATED_BODY()

public:

	UCSFunctionBase() : MethodHandle(nullptr) {}

	// UFunction interface
	virtual void Bind() override;
	// End of UFunction interface
	
	void SetManagedMethod(const TSharedPtr<FGCHandle>& MethodHandle);

	UCSClass* GetOwningManagedClass() const;

protected:
	
	static bool InvokeManagedEvent(UObject* ObjectToInvokeOn, FFrame& Stack, const UCSFunctionBase* Function, uint8* ArgumentBuffer, RESULT_DECL);
	
	TSharedPtr<FGCHandle> MethodHandle;
};
