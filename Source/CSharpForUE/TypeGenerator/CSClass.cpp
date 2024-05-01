#include "CSClass.h"
#include "CSFunction.h"
#include "CSharpForUE/CSDeveloperSettings.h"
#include "CSharpForUE/CSManager.h"
#include "Factories/CSPropertyFactory.h"

#if ENGINE_MINOR_VERSION >= 4
#include "Blueprint/BlueprintExceptionInfo.h"
#endif

void UCSClass::InvokeManagedMethod(UObject* ObjectToInvokeOn, FFrame& Stack, RESULT_DECL)
{
	UCSFunction* Function = CastChecked<UCSFunction>(Stack.CurrentNativeFunction);

	// Skip allocating memory for the argument data if there are no parameters that need to be passed
	if (!Function->NumParms)
	{
		InvokeManagedEvent(ObjectToInvokeOn, Stack, Function, nullptr, RESULT_PARAM);
		return;
	}
	
	FOutParmRec* OutParameters = nullptr;
	FOutParmRec** LastOut = &OutParameters;
	uint8* ArgumentBuffer = Stack.Locals;
	
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
			
			FunctionParameter->CopyCompleteValue(ArgumentData.GetData() + FunctionParameter->GetOffset_ForInternal(), ValueAddress);
		}
	}
	
	if (!InvokeManagedEvent(ObjectToInvokeOn, Stack, Function, ArgumentBuffer, RESULT_PARAM))
	{
		return;
	}
	
	ProcessOutParameters(OutParameters, ArgumentBuffer);

	// Don't free up memory if we're calling this from C++/C#, only Blueprints.
	if (Stack.Code)
	{
		Function->DestroyStruct(ArgumentBuffer);
	}
}

void UCSClass::ProcessOutParameters(FOutParmRec* OutParameters, uint8* ArgumentBuffer)
{
	for (FOutParmRec* OutParameter = OutParameters; OutParameter != nullptr; OutParameter = OutParameter->NextOutParm)
	{
		const uint8* ValueAddress = ArgumentBuffer + OutParameter->Property->GetOffset_ForUFunction();
		OutParameter->Property->CopyCompleteValue(OutParameter->PropAddr, ValueAddress);
	}
}

bool UCSClass::InvokeManagedEvent(UObject* ObjectToInvokeOn, FFrame& Stack, const UCSFunction* Function, uint8* ArgumentBuffer, RESULT_DECL)
{
	if (Stack.Code)
	{
		++Stack.Code;
	}
	
	const FGCHandle ManagedObjectHandle = FCSManager::Get().FindManagedObject(ObjectToInvokeOn);
	FString ExceptionMessage;
	
	bool bSuccess = FCSManagedCallbacks::ManagedCallbacks.InvokeManagedMethod(ManagedObjectHandle.GetHandle(),
		Function->GetManagedMethod(),
		ArgumentBuffer,
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

TSharedRef<FCSharpClassInfo> UCSClass::GetClassInfo() const
{
	return ClassMetaData.ToSharedRef();
}
