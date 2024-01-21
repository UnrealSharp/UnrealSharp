#include "CSClass.h"
#include "CSFunction.h"
#include "CSharpForUE/CSManager.h"
#include "Factories/CSPropertyFactory.h"

void UCSClass::InvokeManagedMethod(UObject* ObjectToInvokeOn, FFrame& Stack, RESULT_DECL)
{
	const UCSFunction* Function = CastChecked<UCSFunction>(Stack.CurrentNativeFunction);
	TArray<uint8> ArgumentData;

	if (Stack.Code)
	{
		++Stack.Code;
	}

	// Skip allocating memory for the argument data if there are no parameters that need to be passed
	if (Function->NumParms == 0)
	{
		InvokeManagedEvent(ObjectToInvokeOn, Function, ArgumentData, RESULT_PARAM);
		return;
	}
	
    void* LocalStruct = FMemory_Alloca(FMath::Max<int32>(1, Function->GetStructureSize()));
    Function->InitializeStruct(LocalStruct);
	
    FOutParmRec* OutParameters = nullptr;
    FOutParmRec** LastOut = &OutParameters;
	
	for (TFieldIterator<FProperty> ParamIt(Function, EFieldIteratorFlags::ExcludeSuper); ParamIt && !(ParamIt->PropertyFlags & CPF_ReturnParm); ++ParamIt)
    {
        FProperty* FunctionParameter = *ParamIt;

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
		
        ArgumentData.Append(ValueAddress, FunctionParameter->GetSize());
    }

	InvokeManagedEvent(ObjectToInvokeOn, Function, ArgumentData, RESULT_PARAM);
	ProcessOutParameters(OutParameters, ArgumentData);
	
	// Free up memory
	Function->DestroyStruct(LocalStruct);
}

void UCSClass::ProcessOutParameters(FOutParmRec* OutParameters, TArray<uint8>& ArgumentData)
{
	uint8* Data = ArgumentData.GetData();
	
	for (FOutParmRec* OutParameter = OutParameters; OutParameter != nullptr; OutParameter = OutParameter->NextOutParm)
	{
		uint8* ValueAddress = Data + OutParameter->Property->GetOffset_ForUFunction();
		OutParameter->Property->CopyCompleteValue(OutParameter->PropAddr, ValueAddress);
	}
}

void UCSClass::InvokeManagedEvent(UObject* ObjectToInvokeOn, const UCSFunction* Function, TArray<uint8>& ArgumentData, RESULT_DECL)
{
	const FGCHandle ManagedObjectHandle = FCSManager::Get().FindManagedObject(ObjectToInvokeOn);
	FCSManagedCallbacks::ManagedCallbacks.InvokeManagedMethod(ManagedObjectHandle.GetHandle(), Function->GetManagedMethod(), ArgumentData.GetData(), RESULT_PARAM);
}
