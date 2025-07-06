#include "CSFunction_Params.h"

void UCSFunction_Params::InvokeManagedMethod_Params(UObject* ObjectToInvokeOn, FFrame& Stack, RESULT_DECL)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSFunction_Params::InvokeManagedMethod_Params);
	
	UFunction* Function = Stack.CurrentNativeFunction;
	FOutParmRec* OutParameters = nullptr;
	FOutParmRec** LastOut = &OutParameters;
	uint8* ArgumentBuffer = Stack.Locals;
	uint8* LocalsCache = Stack.Locals;

	// If we're calling this from BP, we need to copy the parameters to a new buffer
	if (Stack.Code)
	{
		TRACE_CPUPROFILER_EVENT_SCOPE(UCSFunction_Params::InvokeManagedMethod_Params::CopyParametersToBuffer);
		
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

		Stack.Locals = ArgumentBuffer;
	}
	else
	{
		OutParameters = Stack.OutParms;
	}
	
	InvokeManagedMethod(ObjectToInvokeOn, Stack, RESULT_PARAM);
	
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

	// Restore the local pointer so we are still pointing to the right location
	Stack.Locals = LocalsCache;
}
