#include "CSClass.h"
#include "CSFunction.h"
#include "CSharpForUE/CSDeveloperSettings.h"
#include "CSharpForUE/CSManager.h"
#include "Factories/CSPropertyFactory.h"

void UCSClass::InvokeManagedMethod(UObject* ObjectToInvokeOn, FFrame& Stack, RESULT_DECL)
{
	const UCSFunction* Function = CastChecked<UCSFunction>(Stack.CurrentNativeFunction);
	FString ExceptionMessage;
	bool Success;

	// Skip allocating memory for the argument data if there are no parameters that need to be passed
	if (Function->NumParms == 0)
	{
		if (Stack.Code)
		{
			++Stack.Code;
		}
		
		Success = InvokeManagedEvent(ObjectToInvokeOn, Function, TArrayView<const uint8>(), ExceptionMessage, RESULT_PARAM);
		if (!Success)
		{
			const FBlueprintExceptionInfo ExceptionInfo(EBlueprintExceptionType::FatalError, FText::FromString(ExceptionMessage));
			FBlueprintCoreDelegates::ThrowScriptException(ObjectToInvokeOn, Stack, ExceptionInfo);
		}
		return;
	}
	
	int LocalStructSize = Function->GetStructureSize();
    void* LocalStruct = FMemory_Alloca(FMath::Max<int32>(1, LocalStructSize));
    Function->InitializeStruct(LocalStruct);
	
    FOutParmRec* OutParameters = nullptr;
    FOutParmRec** LastOut = &OutParameters;

	TArrayView<uint8> ArgumentData((uint8*)FMemory_Alloca(FMath::Max<int32>(1, LocalStructSize)), LocalStructSize);
	
	for (TFieldIterator<FProperty> ParamIt(Function, EFieldIteratorFlags::ExcludeSuper); ParamIt; ++ParamIt)
    {
        FProperty* FunctionParameter = *ParamIt;

		if (FunctionParameter->HasAnyPropertyFlags(CPF_ReturnParm))
		{
			continue;
		}

		Stack.MostRecentPropertyAddress = nullptr;
		Stack.MostRecentPropertyContainer = nullptr;
		uint8* LocalValue = FunctionParameter->ContainerPtrToValuePtr<uint8>(LocalStruct);
		Stack.StepCompiledIn(LocalValue, FunctionParameter->GetClass());
		
		uint8* ValueAddress;
		
		if (FunctionParameter->HasAnyPropertyFlags(CPF_OutParm) && Stack.MostRecentPropertyAddress)
		{
			ValueAddress = Stack.MostRecentPropertyAddress;
		}
		else
		{
			ValueAddress = LocalValue;
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

		// When C# marshals the parameter to it's own format, it will call GetOffset_ForInternal to determine the offset into ArgumentData to read the parameter from
		// Let's check that the offset matches where we're currently writing into ArgumentData - if it doesn't, C# will be reading from the wrong place and we have ourselves a nasty alignment error!
		int InternalOffset = FunctionParameter->GetOffset_ForInternal();
		int InternalSize = FunctionParameter->GetSize();
		check((InternalOffset + InternalSize) <= ArgumentData.Num());
		FMemory::Memcpy(ArgumentData.GetData() + InternalOffset, ValueAddress, InternalSize);
    }
	
	if (Stack.Code)
	{
		++Stack.Code;
	}
	
	Success = InvokeManagedEvent(ObjectToInvokeOn, Function, ArgumentData, ExceptionMessage, RESULT_PARAM);
	
	if (!Success)
	{
		const UCSDeveloperSettings* Settings = GetDefault<UCSDeveloperSettings>();
		EBlueprintExceptionType::Type ExceptionType = Settings->bCrashOnException ? EBlueprintExceptionType::FatalError : EBlueprintExceptionType::NonFatalError;
		
		const FBlueprintExceptionInfo ExceptionInfo(ExceptionType, FText::FromString(ExceptionMessage));
		FBlueprintCoreDelegates::ThrowScriptException(ObjectToInvokeOn, Stack, ExceptionInfo);
	}
	
	ProcessOutParameters(OutParameters, ArgumentData);
	
	// Free up memory
	Function->DestroyStruct(LocalStruct);
}

void UCSClass::ProcessOutParameters(FOutParmRec* OutParameters, TArrayView<const uint8> ArgumentData)
{
	for (FOutParmRec* OutParameter = OutParameters; OutParameter != nullptr; OutParameter = OutParameter->NextOutParm)
	{
		const uint8* ValueAddress = ArgumentData.GetData() + OutParameter->Property->GetOffset_ForUFunction();
		OutParameter->Property->CopyCompleteValue(OutParameter->PropAddr, ValueAddress);
	}
}

bool UCSClass::InvokeManagedEvent(UObject* ObjectToInvokeOn, const UCSFunction* Function, TArrayView<const uint8> ArgumentData, FString& ExceptionMessage, RESULT_DECL)
{
	const FGCHandle ManagedObjectHandle = FCSManager::Get().FindManagedObject(ObjectToInvokeOn);
	int ResultCode = FCSManagedCallbacks::ManagedCallbacks.InvokeManagedMethod(ManagedObjectHandle.GetHandle(), Function->GetManagedMethod(), (void*)ArgumentData.GetData(), RESULT_PARAM, &ExceptionMessage);
	return ResultCode == 0;
}
