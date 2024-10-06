#include "CSFunction.h"
#include "CSDeveloperSettings.h"
#include "CSManagedGCHandle.h"
#include "CSManager.h"

#if ENGINE_MINOR_VERSION >= 4
#include "Blueprint/BlueprintExceptionInfo.h"
#endif

void UCSFunctionBase::SetManagedMethod(void* InManagedMethod)
{
	if (ManagedMethod)
	{
		return;
	}
	
	ManagedMethod = InManagedMethod;
}

bool UCSFunctionBase::InvokeManagedEvent(UObject* ObjectToInvokeOn, FFrame& Stack, const UCSFunctionBase* Function, uint8* ArgumentBuffer, RESULT_DECL)
{
	UCSManager& Manager = UCSManager::Get();
	
	Manager.SetCurrentWorldContext(ObjectToInvokeOn);
	
	if (Stack.Code)
	{
		++Stack.Code;
	}
	
	const FGCHandle ManagedObjectHandle = Manager.FindManagedObject(ObjectToInvokeOn);
	FString ExceptionMessage;
	
	bool bSuccess = FCSManagedCallbacks::ManagedCallbacks.InvokeManagedMethod(ManagedObjectHandle.GetHandle(),
		Function->ManagedMethod,
		ArgumentBuffer,
		RESULT_PARAM,
		&ExceptionMessage) == 0;

#if WITH_EDITOR
	if (!bSuccess)
	{
		const UCSDeveloperSettings* Settings = GetDefault<UCSDeveloperSettings>();
		EBlueprintExceptionType::Type ExceptionType = Settings->bCrashOnException ? EBlueprintExceptionType::FatalError : EBlueprintExceptionType::NonFatalError;
		
		const FBlueprintExceptionInfo ExceptionInfo(ExceptionType, FText::FromString(ExceptionMessage));
		FBlueprintCoreDelegates::ThrowScriptException(ObjectToInvokeOn, Stack, ExceptionInfo);
	}
#endif

	Manager.SetCurrentWorldContext(nullptr);
	return bSuccess;
}
