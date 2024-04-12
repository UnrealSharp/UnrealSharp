#pragma once

#include "CoreMinimal.h"
#include "UObject/Package.h"
#include "Engine/BlueprintGeneratedClass.h"
#include "CSClass.generated.h"

class UCSFunction;

UCLASS()
class CSHARPFORUE_API UCSClass : public UBlueprintGeneratedClass
{
	GENERATED_BODY()

public:

	static void InvokeManagedMethod(UObject* ObjectToInvokeOn, FFrame& Stack, RESULT_DECL);
	static void ProcessOutParameters(FOutParmRec* OutParameters, TArrayView<const uint8> ArgumentData);
	static bool InvokeManagedEvent(UObject* ObjectToInvokeOn, FFrame& Stack, const UCSFunction* Function, TArrayView<const uint8> ArgumentData, RESULT_DECL);

	bool bCanTick = true;
};
