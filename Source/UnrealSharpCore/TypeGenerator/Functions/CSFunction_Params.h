#pragma once

#include "CoreMinimal.h"
#include "CSFunction.h"
#include "CSFunction_Params.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSFunction_Params : public UCSFunctionBase
{
	GENERATED_BODY()
public:
	static void InvokeManagedMethod_Params(UObject* ObjectToInvokeOn, FFrame& Stack, RESULT_DECL);
	static bool IsOutParameter(const FProperty* InParam)
	{
		const bool bIsParam = InParam->HasAnyPropertyFlags(CPF_Parm);
		const bool bIsReturnParam = InParam->HasAnyPropertyFlags(CPF_ReturnParm);
		const bool bIsOutParam = InParam->HasAnyPropertyFlags(CPF_OutParm) && !InParam->HasAnyPropertyFlags(CPF_ConstParm);
		return bIsParam && !bIsReturnParam && bIsOutParam;
	}
};
