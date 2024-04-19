#include "CSClass.h"
#include "CSFunction.h"
#include "CSharpForUE/CSDeveloperSettings.h"
#include "CSharpForUE/CSManager.h"
#include "Factories/CSPropertyFactory.h"

void UCSClass::InvokeManagedMethod(UObject* ObjectToInvokeOn, FFrame& Stack, RESULT_DECL)
{
	UCSFunction* Function = CastChecked<UCSFunction>(Stack.CurrentNativeFunction);

	// Skip allocating memory for the argument data if there are no parameters that need to be passed
	if (Function->NumParms == 0)
	{
		InvokeManagedEvent(ObjectToInvokeOn, Stack, Function, nullptr, RESULT_PARAM);
		return;
	}

	FOutParmRec* OutParameters = nullptr;
	FOutParmRec** LastOut = &OutParameters;
	
	uint8* ParamBuffer;

	// Called by BP
	if (Stack.Code)
	{
		ParamBuffer = static_cast<uint8*>(FMemory_Alloca(Function->ParmsSize));
		for (TFieldIterator<FProperty> ParamIt(Function, EFieldIteratorFlags::ExcludeSuper); ParamIt; ++ParamIt)
		{
			FProperty* FunctionParameter = *ParamIt;

			if (FunctionParameter->HasAnyPropertyFlags(CPF_ReturnParm))
			{
				continue;
			}

			Stack.MostRecentPropertyAddress = nullptr;
			Stack.MostRecentPropertyContainer = nullptr;
		
			uint8* LocalValue = ParamBuffer + FunctionParameter->GetOffset_ForUFunction();
			Stack.StepCompiledIn(Stack.Object, FunctionParameter->GetClass());
		
			uint8* ValueAddress = LocalValue;
			if (FunctionParameter->HasAnyPropertyFlags(CPF_OutParm) && Stack.MostRecentPropertyAddress)
			{
				ValueAddress = Stack.MostRecentPropertyAddress;
			}

			// Add any output parameters to the output params chain
			if (FCSPropertyFactory::IsOutParameter(FunctionParameter))
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

			int InternalOffset = FunctionParameter->GetOffset_ForInternal();
			int InternalSize = FunctionParameter->GetSize();
			FMemory::Memcpy(ParamBuffer + InternalOffset, ValueAddress, InternalSize);
		}
	}
	else
	{
		ParamBuffer = Stack.Locals;
	}
	
	if (!InvokeManagedEvent(ObjectToInvokeOn, Stack, Function, ParamBuffer, RESULT_PARAM))
	{
		return;
	}
	
	ProcessOutParameters(OutParameters, ParamBuffer);
	
	// Free up memory
	Function->DestroyStruct(ParamBuffer);
}

void UCSClass::ProcessOutParameters(FOutParmRec* OutParameters, const uint8* ArgumentData)
{
	for (FOutParmRec* OutParameter = OutParameters; OutParameter != nullptr; OutParameter = OutParameter->NextOutParm)
	{
		const uint8* ValueAddress = ArgumentData + OutParameter->Property->GetOffset_ForUFunction();
		OutParameter->Property->CopyCompleteValue(OutParameter->PropAddr, ValueAddress);
	}
}

bool UCSClass::InvokeManagedEvent(UObject* ObjectToInvokeOn, FFrame& Stack, const UCSFunction* Function, uint8* ArgumentData, RESULT_DECL)
{
	if (Stack.Code)
	{
		P_FINISH;
	}
	
	const FGCHandle ManagedObjectHandle = FCSManager::Get().FindManagedObject(ObjectToInvokeOn);
	FString ExceptionMessage;
	
	bool bSuccess = FCSManagedCallbacks::ManagedCallbacks.InvokeManagedMethod(ManagedObjectHandle.GetHandle(),
		Function->GetManagedMethod(),
		ArgumentData,
		RESULT_PARAM,
		&ExceptionMessage) == 0;
	
	if (!bSuccess)
	{
		const UCSDeveloperSettings* Settings = GetDefault<UCSDeveloperSettings>();
		EBlueprintExceptionType::Type ExceptionType = Settings->bCrashOnException ? EBlueprintExceptionType::FatalError : EBlueprintExceptionType::NonFatalError;
		
		const FBlueprintExceptionInfo ExceptionInfo(ExceptionType, FText::FromString(ExceptionMessage));
		FBlueprintCoreDelegates::ThrowScriptException(ObjectToInvokeOn, Stack, ExceptionInfo);
	}
	
	return bSuccess;
}
