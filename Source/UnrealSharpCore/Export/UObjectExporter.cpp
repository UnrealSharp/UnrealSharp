#include "UObjectExporter.h"
#include "UnrealSharpCore/CSManager.h"
#include "UObject/UObjectGlobals.h"


void* UUObjectExporter::CreateNewObject(UObject* Outer, UClass* Class, UObject* Template)
{
	if (!IsValid(Outer))
	{
		return nullptr;
	}
	
	UObject* NewCSharpObject = NewObject<UObject>(Outer, Class, NAME_None, RF_NoFlags, Template);
	return UCSManager::Get().FindManagedObject(NewCSharpObject);
}

void* UUObjectExporter::GetTransientPackage()
{
	UPackage* TransientPackage = ::GetTransientPackage();

	if (!IsValid(TransientPackage))
	{
		return nullptr;
	}

	return UCSManager::Get().FindManagedObject(TransientPackage);
}

void UUObjectExporter::NativeGetName(UObject* Object, FName* OutName)
{
	*OutName = !IsValid(Object) ? NAME_None : Object->GetFName();
}

void EvaluateInvokePath(UObject* NativeObject, UFunction* NativeFunction, uint8* Params, uint8* ReturnValueAddress)
{
	if (NativeFunction->HasAllFunctionFlags(FUNC_HasOutParms))
	{
		UUObjectExporter::InvokeNativeFunctionOutParms(NativeObject, NativeFunction, Params, ReturnValueAddress);
	}
    //if the function is an event and not native we would go through UObject::ProcessEvent to avoid stack corruption since it will call into BP code
	else if (!NativeFunction->HasAnyFunctionFlags(FUNC_Native) && NativeFunction->HasAnyFunctionFlags(FUNC_Event))
	{
		NativeObject->ProcessEvent(NativeFunction, Params);
	}
    //if the function is native we can go through the fast path. it could also contain the event flag which is common for u# functions
	else
	{
		FFrame NewStack(NativeObject, NativeFunction, Params, nullptr, NativeFunction->ChildProperties);
		NativeFunction->Invoke(NativeObject, NewStack, ReturnValueAddress);
	}
}

void UUObjectExporter::InvokeNativeFunction(UObject* NativeObject, UFunction* NativeFunction, uint8* Params, uint8* ReturnValueAddress)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UUObjectExporter::InvokeNativeFunction);

    EvaluateInvokePath(NativeObject, NativeFunction, Params, ReturnValueAddress);
}

void UUObjectExporter::InvokeNativeStaticFunction(UClass* NativeClass, UFunction* NativeFunction, uint8* Params, uint8* ReturnValueAddress)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UUObjectExporter::InvokeNativeStaticFunction);

	UObject* ClassDefaultObject = NativeClass->GetDefaultObject();
	EvaluateInvokePath(ClassDefaultObject, NativeFunction, Params, ReturnValueAddress);
}

void UUObjectExporter::InvokeNativeNetFunction(UObject* NativeObject, UFunction* NativeFunction, uint8* Params, uint8* ReturnValueAddress)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UUObjectExporter::InvokeNativeNetFunction);

	int32 FunctionCallspace = NativeObject->GetFunctionCallspace(NativeFunction, nullptr);

	if (FunctionCallspace & FunctionCallspace::Remote)
	{
		NativeObject->CallRemoteFunction(NativeFunction, Params, nullptr, nullptr);
		return;
	}

	if (FunctionCallspace & FunctionCallspace::Absorbed)
	{
		return;
	}

	EvaluateInvokePath(NativeObject, NativeFunction, Params, ReturnValueAddress);
}

void UUObjectExporter::InvokeNativeFunctionOutParms(UObject* NativeObject, UFunction* NativeFunction, uint8* Params, uint8* ReturnValueAddress)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UUObjectExporter::InvokeNativeFunctionOutParms);

    //choose a most fast path. the assumption is that functions defined in U# which are not overridden in BP code can choose a fast path.
    //if the function calls into BP code we need dedicated stack memory to not risk a stack corruption for BP to use.
	if (!NativeFunction->HasAnyFunctionFlags(FUNC_Native) && NativeFunction->HasAnyFunctionFlags(FUNC_Event))
	{
		NativeObject->ProcessEvent(NativeFunction, Params);
	}
	else
	{
		FFrame NewStack(NativeObject, NativeFunction, Params, nullptr, NativeFunction->ChildProperties);
		FOutParmRec** LastOut = &NewStack.OutParms;

		for (TFieldIterator<FProperty> PropIt(NativeFunction); PropIt; ++PropIt)
		{
			FProperty* Property = *PropIt;

			if (!Property->HasAllPropertyFlags(CPF_OutParm))
			{
				continue;
			}

			FOutParmRec* Out = static_cast<FOutParmRec*>(UE_VSTACK_ALLOC(VirtualStackAllocator, sizeof(FOutParmRec)));

			Out->PropAddr = Property->ContainerPtrToValuePtr<uint8>(Params);
			Out->Property = Property;

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

		if (*LastOut)
		{
			(*LastOut)->NextOutParm = nullptr;
		}

		NativeFunction->Invoke(NativeObject, NewStack, ReturnValueAddress);
	}
}

bool UUObjectExporter::NativeIsValid(UObject* Object)
{
	return IsValid(Object);
}

void* UUObjectExporter::GetWorld_Internal(UObject* Object)
{
	if (!IsValid(Object))
	{
		return nullptr;
	}

	UWorld* World = Object->GetWorld();
	return UCSManager::Get().FindManagedObject(World);
}

bool UUObjectExporter::IsA(const UObject* Object, UClass* Class)
{
    return Object->IsA(Class);
}

uint32 UUObjectExporter::GetUniqueID(UObject* Object)
{
	return Object->GetUniqueID();
}

void* UUObjectExporter::StaticLoadClass(UClass* BaseClass, UObject* InOuter, const char* Name)
{
	if (Name == nullptr)
	{
		return nullptr;
	}

	UClass* Loaded = ::StaticLoadClass(BaseClass, InOuter, UTF8_TO_TCHAR(Name));
	if (!IsValid(Loaded))
	{
		return nullptr;
	}
	return UCSManager::Get().FindManagedObject(Loaded);
}


void* UUObjectExporter::StaticLoadObject(UClass* BaseClass, UObject* InOuter, const char* Name)
{
	if (Name == nullptr)
	{
		return nullptr;
	}

	UObject* LoadedObj = ::StaticLoadObject(BaseClass, InOuter, UTF8_TO_TCHAR(Name));
	if (!IsValid(LoadedObj))
	{
		return nullptr;
	}
	return UCSManager::Get().FindManagedObject(LoadedObj);
}
