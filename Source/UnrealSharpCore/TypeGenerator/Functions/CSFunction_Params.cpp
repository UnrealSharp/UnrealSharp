#include "CSFunction_Params.h"

void UCSFunction_Params::InvokeManagedMethod_Params(UObject* ObjectToInvokeOn, FFrame& Stack, RESULT_DECL)
{
	UCSFunctionBase* Function = static_cast<UCSFunctionBase*>(Stack.CurrentNativeFunction);
	FOutParmRec* OutParameters = nullptr;
	FOutParmRec** LastOut = &OutParameters;
	uint8* ArgumentBuffer = Stack.Locals;

	// If we're calling this from BP, we need to copy the parameters to a new buffer
	if (Stack.Code)
	{
		int LocalStructSize = Function->GetStructureSize();
		TArrayView<uint8> ArgumentData((uint8*)FMemory_Alloca(FMath::Max<int32>(1, LocalStructSize)), LocalStructSize);
		ArgumentBuffer = ArgumentData.GetData();
		Function->InitializeStruct(ArgumentBuffer);
	
		for (TFieldIterator<FProperty> ParamIt(Function, EFieldIteratorFlags::ExcludeSuper); ParamIt; ++ParamIt)
		{
			FProperty* FunctionParameter = *ParamIt;

			if (FunctionParameter->HasAnyPropertyFlags(CPF_ReturnParm))
			{
				continue;
			}

			Stack.MostRecentPropertyAddress = nullptr;
			Stack.MostRecentPropertyContainer = nullptr;
			uint8* LocalValue = FunctionParameter->ContainerPtrToValuePtr<uint8>(ArgumentData.GetData());
			Stack.StepCompiledIn(LocalValue, FunctionParameter->GetClass());
		
			uint8* ValueAddress = LocalValue;
			if (FunctionParameter->HasAnyPropertyFlags(CPF_OutParm) && Stack.MostRecentPropertyAddress)
			{
				ValueAddress = Stack.MostRecentPropertyAddress;
			}

			// Add any output parameters to the output params chain
			if (IsOutParameter(FunctionParameter))
			{
				FOutParmRec* Out = static_cast<FOutParmRec*>(FMemory_Alloca(sizeof(FOutParmRec)));
				Out->Property = FunctionParameter;
				Out->PropAddr = ValueAddress;
				Out->NextOutParm = nullptr;

				// Link it to the end of the list
				if (*LastOut)
				{
					(*LastOut)->NextOutParm = Out;
					LastOut = &(*LastOut)->NextOutParm;
				}
				else
				{
					*LastOut = Out;
				}
			}
			
			FunctionParameter->CopyCompleteValue(ArgumentData.GetData() + FunctionParameter->GetOffset_ForInternal(), ValueAddress);
		}
	}
	
	if (!InvokeManagedEvent(ObjectToInvokeOn, Stack, Function, ArgumentBuffer, RESULT_PARAM))
	{
		return;
	}
	
	for (FOutParmRec* OutParameter = OutParameters; OutParameter != nullptr; OutParameter = OutParameter->NextOutParm)
	{
		const uint8* ValueAddress = ArgumentBuffer + OutParameter->Property->GetOffset_ForUFunction();
		OutParameter->Property->CopyCompleteValue(OutParameter->PropAddr, ValueAddress);
	}

	// Only free up the buffer if we're calling from BP
	if (Stack.Code)
	{
		Function->DestroyStruct(ArgumentBuffer);
	}
}

bool UCSFunction_Params::IsOutParameter(const FProperty* InParam)
{
	const bool bIsParam = InParam->HasAnyPropertyFlags(CPF_Parm);
	const bool bIsReturnParam = InParam->HasAnyPropertyFlags(CPF_ReturnParm);
	const bool bIsOutParam = InParam->HasAnyPropertyFlags(CPF_OutParm) && !InParam->HasAnyPropertyFlags(CPF_ConstParm);
	return bIsParam && !bIsReturnParam && bIsOutParam;
}
