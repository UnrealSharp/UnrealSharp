#include "Functions/CSFunction.h"
#include "CSManagedGCHandle.h"
#include "CSManager.h"
#include "CSUnrealSharpSettings.h"
#include "Types/CSClass.h"
#include "Types/CSSkeletonClass.h"
#include "Engine/World.h"

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
		if (Function.Name != GetFName())
		{
			continue;
		}
		
		SetNativeFunc(Function.Pointer);
		return;
	}
}

bool UCSFunctionBase::UpdateMethodHandle()
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSFunctionBase::TryUpdateMethodHandle);
	
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

	if (WorldContext)
	{
		UCSManager::Get().SetCurrentWorldContext(WorldContext);
	}

	UCSFunctionBase* ManagedFunction = static_cast<UCSFunctionBase*>(Stack.CurrentNativeFunction);

#if WITH_EDITOR
	// After a full reload, method pointers are stale, so we just lazy update them here.
	if (!ManagedFunction->HasValidMethodHandle() && !ManagedFunction->UpdateMethodHandle())
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
#if ENGINE_MAJOR_VERSION >= 5 && ENGINE_MINOR_VERSION >= 6
	const EBlueprintExceptionType::Type ExceptionType = Settings->bCrashOnException ? EBlueprintExceptionType::FatalError : EBlueprintExceptionType::UserRaisedError;
#else
	const EBlueprintExceptionType::Type ExceptionType = EBlueprintExceptionType::FatalError;
#endif
	const FBlueprintExceptionInfo ExceptionInfo(ExceptionType, FText::FromString(ExceptionMessage));
	FBlueprintCoreDelegates::ThrowScriptException(ObjectToInvokeOn, Stack, ExceptionInfo);
}
