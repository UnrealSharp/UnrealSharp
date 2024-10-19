#pragma once

#include "CoreMinimal.h"
#include "CSFunction.h"
#include "CSFunction_NoParams.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSFunction_NoParams : public UCSFunctionBase
{
	GENERATED_BODY()

public:
	
	static void InvokeManagedMethod_NoParams(UObject* ObjectToInvokeOn, FFrame& Stack, RESULT_DECL);
	
};
