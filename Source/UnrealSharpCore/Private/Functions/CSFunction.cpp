#include "Functions/CSFunction.h"
#include "Runtime/Launch/Resources/Version.h"
#include "Functions/CSFunction_Params.h"
#include "CSManagedGCHandle.h"
#include "CSManager.h"
#include "CSUnrealSharpSettings.h"
#include "Types/CSClass.h"
#include "Engine/World.h"

#include "Blueprint/BlueprintExceptionInfo.h"

void UCSFunctionBase::Bind()
{
	// Reinstanced classes do not retain NativeFunctionLookupTable. Bind the same
	// managed thunk selected by the function factory, including on duplicated functions.
	SetNativeFunc(NumParms == 0 ? &UCSFunctionBase::InvokeManagedMethod : &UCSFunction_Params::InvokeManagedMethod_Params);
}

bool UCSFunctionBase::UpdateMethodHandle()
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSFunctionBase::UpdateMethodHandle);
	
	// Ignore delegate signatures and classes that are not the generated class.
	// The Blueprint skeleton class is an example of a class that is not the generated class, but still has managed functions.
	if (HasValidMethodHandle() || !IsOwnedByManagedClass() || GetOwnerClass()->HasAllClassFlags(CLASS_Interface))
	{
		return true;
	}
	
	UCSClass* ManagedClass = static_cast<UCSClass*>(GetOwnerClass());
	UCSManagedAssembly* Assembly = ManagedClass->GetOwningAssembly();
	
	TSharedPtr<FCSManagedTypeDefinition> ClassInfo = ManagedClass->GetManagedTypeDefinition();
	TSharedPtr<FGCHandle> TypeHandle = ClassInfo->GetTypeGCHandle();
	
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

	// Prefer using World as context since it's more stable
	UObject* WorldContext = nullptr;
	if (Stack.Object)
	{
		UWorld* World = Stack.Object->GetWorld();
		WorldContext = World ? World : Stack.Object;
	}

	UCSFunctionBase* ManagedFunction = static_cast<UCSFunctionBase*>(Stack.CurrentNativeFunction);

#if WITH_EDITOR
	// After a full reload, method pointers are stale, so we just lazy update them here.
	if (!ManagedFunction->HasValidMethodHandle() && !ManagedFunction->UpdateMethodHandle())
	{
		return;
	}
#endif

	FString ExceptionMessage;
	int32 ReturnCode;
	{
		UCSManager::FCSCurrentWorldContext CurrentWorldContext(WorldContext);
		ReturnCode = GetManagedCallbacks().InvokeManagedMethod(
			UCSManager::Get().FindManagedObject(ObjectToInvokeOn).GetPointer(),
			ManagedFunction->MethodHandle->GetPointer(),
			Stack.Locals,
			RESULT_PARAM,
			&ExceptionMessage);
	}
	
	if (ReturnCode == 0)
	{
		return;
	}

#if ENGINE_MAJOR_VERSION >= 5 && ENGINE_MINOR_VERSION >= 6
	const EBlueprintExceptionType::Type ExceptionType = GetDefault<UCSUnrealSharpSettings>()->bCrashOnException ? EBlueprintExceptionType::FatalError : EBlueprintExceptionType::UserRaisedError;
#else
	const EBlueprintExceptionType::Type ExceptionType = EBlueprintExceptionType::FatalError;
#endif
	
	const FBlueprintExceptionInfo ExceptionInfo(ExceptionType, FText::FromString(ExceptionMessage));
	FBlueprintCoreDelegates::ThrowScriptException(ObjectToInvokeOn, Stack, ExceptionInfo);
}
