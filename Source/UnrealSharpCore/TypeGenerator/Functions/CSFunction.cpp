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
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSFunctionBase::TryUpdateMethodHandle);
	
	// Ignore delegate signatures and classes that are not the generated class.
	// The Blueprint skeleton class is an example of a class that is not the generated class, but still has managed functions.
	if (HasValidMethodHandle() || !IsOwnedByManagedClass() || GetOwnerClass()->HasAllClassFlags(CLASS_Interface))
	{
		return true;
	}
	
	UCSClass* ManagedClass = static_cast<UCSClass*>(GetOwnerClass());
	UCSAssembly* Assembly = ManagedClass->GetOwningAssembly();
	
	TSharedPtr<FCSManagedTypeInfo> ClassInfo = ManagedClass->GetManagedTypeInfo();
	TSharedPtr<FGCHandle> TypeHandle = ClassInfo->GetManagedTypeHandle();
	
	MethodHandle = Assembly->GetManagedMethod(TypeHandle, FString::Printf(TEXT("Invoke_%s"), *GetName()));
	return MethodHandle.IsValid();
}

bool UCSFunctionBase::IsOwnedByManagedClass() const
{
#if WITH_EDITOR
	return FCSClassUtilities::IsManagedClass(GetOwnerClass());
#else
		return true;
#endif
}

void UCSFunctionBase::InvokeManagedMethod(UObject* ObjectToInvokeOn, FFrame& Stack, RESULT_DECL)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSFunctionBase::InvokeManagedMethod);
	
	Stack.Code += !!Stack.Code;

	UCSManager::Get().SetCurrentWorldContext(Stack.Object);

	UCSFunctionBase* ManagedFunction = static_cast<UCSFunctionBase*>(Stack.CurrentNativeFunction);

#if WITH_EDITOR
	// After a full reload, method pointers are stale, so we just lazy update them here.
	if (!ManagedFunction->HasValidMethodHandle() && !ManagedFunction->TryUpdateMethodHandle())
	{
		return;
	}
#endif

	const FGCHandle ManagedObjectHandle = UCSManager::Get().FindManagedObject(ObjectToInvokeOn);
	void* MethodPtr = ManagedFunction->MethodHandle->GetPointer();
	void* ManagedObjectPtr = ManagedObjectHandle.GetPointer();

	FString ExceptionMessage;
	int ReturnCode = FCSManagedCallbacks::ManagedCallbacks.InvokeManagedMethod(
		ManagedObjectPtr,
		MethodPtr,
		Stack.Locals,
		RESULT_PARAM,
		&ExceptionMessage);
	
	if (ReturnCode == 0)
	{
		return;
	}
	
	const UCSUnrealSharpSettings* Settings = GetDefault<UCSUnrealSharpSettings>();
	const EBlueprintExceptionType::Type ExceptionType = Settings->bCrashOnException ? EBlueprintExceptionType::FatalError : EBlueprintExceptionType::NonFatalError;

	const FBlueprintExceptionInfo ExceptionInfo(ExceptionType, FText::FromString(ExceptionMessage));
	FBlueprintCoreDelegates::ThrowScriptException(ObjectToInvokeOn, Stack, ExceptionInfo);
}
