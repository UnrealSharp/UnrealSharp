#include "Functions/CSFunction_Params.h"

void UCSFunction_Params::InvokeManagedMethod_Params(UObject* ObjectToInvokeOn, FFrame& Stack, RESULT_DECL)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSFunction_Params::InvokeManagedMethod_Params);
	
	FOutParmRec* OutParameters = nullptr;
	FOutParmRec** LastOut = &OutParameters;
	uint8* ArgumentBuffer = Stack.Locals;
	uint8* LocalsCache = Stack.Locals;
	bool IsCalledFromBlueprint = Stack.Code != nullptr;
	
	if (IsCalledFromBlueprint)
	{
		TRACE_CPUPROFILER_EVENT_SCOPE(UCSFunction_Params::InvokeManagedMethod_Params::CopyParametersToBuffer);
		
		ArgumentBuffer = (uint8*)FMemory_Alloca(FMath::Max<int32>(1, Stack.CurrentNativeFunction->GetStructureSize()));
		Stack.CurrentNativeFunction->InitializeStruct(ArgumentBuffer);
	
		for (TFieldIterator<FProperty> ParamIt(Stack.CurrentNativeFunction, EFieldIteratorFlags::ExcludeSuper); ParamIt; ++ParamIt)
		{
			FProperty* FunctionParameter = *ParamIt;

			const EPropertyFlags ParamFlags = FunctionParameter->GetPropertyFlags();

			if (ParamFlags & CPF_ReturnParm)
			{
				continue;
			}

			Stack.MostRecentPropertyAddress = nullptr;
			Stack.MostRecentPropertyContainer = nullptr;
			
			uint8* LocalValue = FunctionParameter->ContainerPtrToValuePtr<uint8>(ArgumentBuffer);
			Stack.StepCompiledIn(LocalValue, FunctionParameter->GetClass());
		
			uint8* ValueAddress = LocalValue;
			
			if ((ParamFlags & CPF_OutParm) && Stack.MostRecentPropertyAddress)
			{
				ValueAddress = Stack.MostRecentPropertyAddress;
			}
			
			constexpr EPropertyFlags Relevant = CPF_Parm | CPF_ReturnParm | CPF_OutParm | CPF_ConstParm;
			constexpr EPropertyFlags Wanted = CPF_Parm | CPF_OutParm;
			
			if ((ParamFlags & Relevant) == Wanted)
			{
				FOutParmRec* Out = static_cast<FOutParmRec*>(FMemory_Alloca(sizeof(FOutParmRec)));
				Out->Property = FunctionParameter;
				Out->PropAddr = ValueAddress;
				Out->NextOutParm = nullptr;
				
				*LastOut = Out;
				LastOut = &Out->NextOutParm;
			}
			
			if (ValueAddress != LocalValue)
			{
				FunctionParameter->CopyCompleteValue(LocalValue, ValueAddress);
			}
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
		if (OutParameter->Property->HasAnyPropertyFlags(CPF_ReturnParm))
		{
			continue;
		}
		
		const uint8* ValueAddress = OutParameter->Property->ContainerPtrToValuePtr<uint8>(ArgumentBuffer);
		if (OutParameter->PropAddr != ValueAddress)
		{
			OutParameter->Property->CopyCompleteValue(OutParameter->PropAddr, ValueAddress);
		}
	}
	
	if (IsCalledFromBlueprint)
	{
		Stack.CurrentNativeFunction->DestroyStruct(ArgumentBuffer);
	}

	Stack.Locals = LocalsCache;
}