#include "CSFunction.h"
#include "CSManagedGCHandle.h"
#include "CSManager.h"
#include "CSUnrealSharpSettings.h"
#include "TypeGenerator/CSClass.h"
#include "TypeGenerator/CSSkeletonClass.h"
#include "TypeGenerator/Register/TypeInfo/CSClassInfo.h"

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
	if (HasValidMethodHandle() || !IsOwnedByManagedClass() || GetOwnerClass()->HasAllClassFlags(CLASS_Interface))
	{
		return true;
	}
	
	UCSClass* ManagedClass = static_cast<UCSClass*>(GetOwnerClass());
	TSharedPtr<FCSAssembly> Assembly = ManagedClass->GetTypeInfo()->OwningAssembly;
	
	const FString InvokeMethodName = FString::Printf(TEXT("Invoke_%s"), *GetName());
	TSharedPtr<FCSClassInfo> ClassInfo = ManagedClass->GetTypeInfo();
	TSharedPtr<FGCHandle> TypeHandle = ClassInfo->GetManagedTypeHandle();
	
	MethodHandle = Assembly->GetManagedMethod(TypeHandle, InvokeMethodName);
	return MethodHandle.IsValid();
}

bool UCSFunctionBase::IsOwnedByManagedClass() const
{
#if WITH_EDITOR
	return FCSClassUtilities::IsManagedType(GetOwnerClass());
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
	// Full reload causes the method pointers to become invalid, lazy rebind them, if needed.
	if (!ManagedFunction->HasValidMethodHandle() && !ManagedFunction->TryUpdateMethodHandle())
	{
		return;
	}
#endif

	FGCHandle ManagedObjectHandle = UCSManager::Get().FindManagedObject(ObjectToInvokeOn);
	
	FString ExceptionMessage;
	if (!FCSManagedCallbacks::ManagedCallbacks.InvokeManagedMethod(ManagedObjectHandle.GetPointer(),
		ManagedFunction->MethodHandle->GetPointer(),
		Stack.Locals,
		RESULT_PARAM,
		&ExceptionMessage))
	{
		return;
	}
	
	const UCSUnrealSharpSettings* Settings = GetDefault<UCSUnrealSharpSettings>();
	EBlueprintExceptionType::Type ExceptionType = Settings->bCrashOnException ? EBlueprintExceptionType::FatalError : EBlueprintExceptionType::NonFatalError;
		
	const FBlueprintExceptionInfo ExceptionInfo(ExceptionType, FText::FromString(ExceptionMessage));
	FBlueprintCoreDelegates::ThrowScriptException(ObjectToInvokeOn, Stack, ExceptionInfo);
}
