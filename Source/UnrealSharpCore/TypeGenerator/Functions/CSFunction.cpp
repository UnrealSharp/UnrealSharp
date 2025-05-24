#include "CSFunction.h"
#include "CSManagedGCHandle.h"
#include "CSManager.h"
#include "CSUnrealSharpSettings.h"
#include "UnrealSharpCore.h"
#include "TypeGenerator/CSClass.h"
#include "TypeGenerator/CSSkeletonClass.h"

#if ENGINE_MINOR_VERSION >= 4
#include "Blueprint/BlueprintExceptionInfo.h"
#endif

void UCSFunctionBase::Bind()
{
	UClass* ClassToFindFunction = GetOwnerClass();

#if WITH_EDITOR
	// Redirect to the generated class if we're trying to bind a function in a skeleton class.
	// Since NativeFunctionLookupTable is not copied over when duplicating for reinstancing due to not being a UPROPERTY.
	if (UCSSkeletonClass* OwnerClass = Cast<UCSSkeletonClass>(GetOuter()))
	{
		ClassToFindFunction = OwnerClass->GetGeneratedClass();
	}
#endif

	for (FNativeFunctionLookup& Function : ClassToFindFunction->NativeFunctionLookupTable)
	{
		if (Function.Name == GetFName())
		{
			SetNativeFunc(Function.Pointer);
		}
	}
}

bool UCSFunctionBase::TryUpdateMethodHandle()
{
	// Ignore delegate signatures and classes that are not the generated class.
	// The Blueprint skeleton class is an example of a class that is not the generated class, but still has managed functions.
	if (MethodHandle.IsValid() || HasAllFunctionFlags(FUNC_Delegate) || !IsOwnedByGeneratedClass() || GetOwnerClass()->HasAllClassFlags(CLASS_Interface))
	{
		return true;
	}
	
	UCSClass* ManagedClass = GetOwningManagedClass();
	TSharedPtr<FCSAssembly> Assembly = ManagedClass->GetOwningAssembly();
	
	const FString InvokeMethodName = FString::Printf(TEXT("Invoke_%s"), *GetName());
	MethodHandle = Assembly->GetManagedMethod(ManagedClass, InvokeMethodName);

	if (!MethodHandle.IsValid())
	{
		UE_LOG(LogUnrealSharp, Fatal, TEXT("Failed to find managed method for %s"), *GetName());
		return false;
	}
	
	return true;
}

bool UCSFunctionBase::InvokeManagedEvent(UObject* ObjectToInvokeOn, FFrame& Stack, UCSFunctionBase* Function, uint8* ArgumentBuffer, RESULT_DECL)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSFunctionBase::InvokeManagedEvent);
	Stack.Code += !!Stack.Code;
	
	UCSManager::Get().SetCurrentWorldContext(Stack.Object);

#if WITH_EDITOR
	if (!Function->MethodHandle.IsValid() && !Function->TryUpdateMethodHandle())
	{
		return false;
	}
#endif

	FGCHandle ManagedObjectHandle = UCSManager::Get().FindManagedObject(ObjectToInvokeOn);
	
	FString ExceptionMessage;
	const bool bSuccess = Function->MethodHandle.Invoke(ManagedObjectHandle, ArgumentBuffer, RESULT_PARAM, ExceptionMessage);
	
	if (!bSuccess)
	{
		const UCSUnrealSharpSettings* Settings = GetDefault<UCSUnrealSharpSettings>();
		EBlueprintExceptionType::Type ExceptionType = Settings->bCrashOnException ? EBlueprintExceptionType::FatalError : EBlueprintExceptionType::NonFatalError;
		
		const FBlueprintExceptionInfo ExceptionInfo(ExceptionType, FText::FromString(ExceptionMessage));
		FBlueprintCoreDelegates::ThrowScriptException(ObjectToInvokeOn, Stack, ExceptionInfo);
	}
	
	return bSuccess;
}

UCSClass* UCSFunctionBase::GetOwningManagedClass() const
{
	return Cast<UCSClass>(GetOwnerClass());
}

bool UCSFunctionBase::IsOwnedByGeneratedClass() const
{
#if WITH_EDITOR
	return GetOwningManagedClass() != nullptr;
#else
	return true;
#endif
}
