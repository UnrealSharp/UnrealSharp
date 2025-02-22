#include "CSFunction.h"
#include "CSManagedGCHandle.h"
#include "CSManager.h"
#include "CSUnrealSharpSettings.h"
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

	UCSManager::Get().OnClassReloadedEvent().AddUObject(this, &UCSFunctionBase::OnClassReloaded);
#endif

	for (FNativeFunctionLookup& Function : ClassToFindFunction->NativeFunctionLookupTable)
	{
		if (Function.Name == GetFName())
		{
			SetNativeFunc(Function.Pointer);
		}
	}
}

void UCSFunctionBase::UpdateMethodHandle()
{
	if (MethodHandle.IsValid())
	{
		return;
	}
	
	UCSClass* ManagedClass = GetOwningManagedClass();
	TSharedPtr<FCSAssembly> Assembly = ManagedClass->GetOwningAssembly();
	
	const FString InvokeMethodName = FString::Printf(TEXT("Invoke_%s"), *GetName());
	MethodHandle = Assembly->GetManagedMethod(ManagedClass, InvokeMethodName);
	check(MethodHandle.IsValid());
}

bool UCSFunctionBase::InvokeManagedEvent(UObject* ObjectToInvokeOn, FFrame& Stack, const UCSFunctionBase* Function, uint8* ArgumentBuffer, RESULT_DECL)
{
	UCSManager& Manager = UCSManager::Get();
	Manager.SetCurrentWorldContext(ObjectToInvokeOn);
	
	if (Stack.Code)
	{
		++Stack.Code;
	}
	
	FGCHandle ManagedObjectHandle = Manager.FindManagedObject(ObjectToInvokeOn);
	
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
	return GetOwningManagedClass() != nullptr;
}

void UCSFunctionBase::OnClassReloaded(UClass* Class)
{
	if (Class == GetOuter())
	{
		UpdateMethodHandle();
	}
}
