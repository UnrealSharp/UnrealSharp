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
	static void ProcessOutParameters(FOutParmRec* OutParameters, TArray<uint8>& ArgumentData);
	static void InvokeManagedEvent(UObject* ObjectToInvokeOn, const UCSFunction* Function, TArray<uint8>& ArgumentData, RESULT_DECL);

	bool bCanTick = true;
};
