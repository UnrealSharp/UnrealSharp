#include "Functions/CSFunction.h"
#include "CSManagedGCHandle.h"
#include "CSManager.h"
#include "CSUnrealSharpSettings.h"
#include "CSWorldContextScope.h"
#include "Types/CSClass.h"
#include "Types/CSSkeletonClass.h"
#include "UObject/UnrealType.h"

#include "Blueprint/BlueprintExceptionInfo.h"

FObjectPropertyBase* UCSFunctionBase::FindWorldContextProperty(UFunction* Function, bool& bHasWorldContextMetadata)
{
	const FString WorldContextParameterName = Function->GetMetaData(TEXT("WorldContext"));
	bHasWorldContextMetadata = !WorldContextParameterName.IsEmpty();

	if (bHasWorldContextMetadata)
	{
		return FindFProperty<FObjectPropertyBase>(Function, *WorldContextParameterName);
	}

	// Match the names recognized by the managed glue generator for native APIs
	// which omit explicit WorldContext metadata.
	static const FName WorldContextObjectName(TEXT("WorldContextObject"));
	static const FName WorldContextName(TEXT("WorldContext"));
	static const FName ContextObjectName(TEXT("ContextObject"));

	for (TFieldIterator<FProperty> PropertyIt(Function, EFieldIteratorFlags::ExcludeSuper); PropertyIt; ++PropertyIt)
	{
		FObjectPropertyBase* ObjectProperty = CastField<FObjectPropertyBase>(*PropertyIt);
		if (!ObjectProperty || !ObjectProperty->HasAnyPropertyFlags(CPF_Parm) || ObjectProperty->HasAnyPropertyFlags(CPF_ReturnParm))
		{
			continue;
		}

		const FName PropertyName = ObjectProperty->GetFName();
		if (PropertyName == WorldContextObjectName || PropertyName == WorldContextName || PropertyName == ContextObjectName)
		{
			return ObjectProperty;
		}
	}

	return nullptr;
}

void UCSFunctionBase::CacheWorldContextProperty()
{
	bool bHasWorldContextMetadata = false;
	CachedWorldContextProperty = FindWorldContextProperty(this, bHasWorldContextMetadata);
	bHasExplicitWorldContext = CachedWorldContextProperty != nullptr || bHasWorldContextMetadata;
	bWorldContextPropertyCached = true;

	if (!CachedWorldContextProperty && bHasWorldContextMetadata)
	{
		UE_LOGFMT(LogUnrealSharp, Warning,
			"Function {0} declares WorldContext metadata, but parameter {1} is not an object property",
			GetName(), GetMetaData(TEXT("WorldContext")));
	}
}

bool UCSFunctionBase::TryGetExplicitWorldContext(uint8* ParameterBuffer, UObject*& OutWorldContextObject) const
{
	check(bWorldContextPropertyCached);

	if (!bHasExplicitWorldContext)
	{
		return false;
	}

	if (!CachedWorldContextProperty || !ParameterBuffer)
	{
		return true;
	}

	const void* ValueAddress = CachedWorldContextProperty->ContainerPtrToValuePtr<void>(ParameterBuffer);
	OutWorldContextObject = CachedWorldContextProperty->GetObjectPropertyValue(ValueAddress);
	return true;
}

void UCSFunctionBase::Bind()
{
	// Cache our world context property so we don't have to look it up every time we invoke this function.
	CacheWorldContextProperty();

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
	
	MethodHandle = Assembly->FindMethodHandle(TypeHandle, FString::Printf(TEXT("Invoke_%s"), *GetName()));
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
	UCSFunctionBase* ManagedFunction = static_cast<UCSFunctionBase*>(Stack.CurrentNativeFunction);

	UObject* WorldContextObject = nullptr;
	const bool bHasExplicitWorldContext = ManagedFunction->TryGetExplicitWorldContext(Stack.Locals, WorldContextObject);
	if (!bHasExplicitWorldContext)
	{
		WorldContextObject = IsValid(ObjectToInvokeOn) ? ObjectToInvokeOn : Stack.Object;
	}

	// An explicit context, including an explicitly null context.
	// Receiver-less nested calls inherit the caller's active scope.
	FCSWorldContextScope WorldContextScope(WorldContextObject, !bHasExplicitWorldContext);

#if WITH_EDITOR
	// After a full reload, method pointers are stale, so we just lazy update them here.
	if (!ManagedFunction->HasValidMethodHandle() && !ManagedFunction->UpdateMethodHandle())
	{
		return;
	}
#endif

	FString ExceptionMessage;
	int ReturnCode = GetManagedCallbacks().InvokeManagedMethod(
		UCSManager::Get().FindManagedObject(ObjectToInvokeOn).GetPointer(),
		ManagedFunction->MethodHandle->GetPointer(),
		Stack.Locals,
		RESULT_PARAM,
		&ExceptionMessage);
	
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
