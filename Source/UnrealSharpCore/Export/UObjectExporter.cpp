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

void UUObjectExporter::InvokeNativeFunction(UObject* NativeObject, UFunction* NativeFunction, uint8* Params, uint8* ReturnValueAddress)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UUObjectExporter::InvokeNativeFunction);
	FFrame NewStack(NativeObject, NativeFunction, Params, nullptr, NativeFunction->ChildProperties);
	NativeFunction->Invoke(NativeObject, NewStack, ReturnValueAddress);
}

void UUObjectExporter::InvokeNativeStaticFunction(UClass* NativeClass, UFunction* NativeFunction, uint8* Params, uint8* ReturnValueAddress)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UUObjectExporter::InvokeNativeStaticFunction);
	UObject* ClassDefaultObject = NativeClass->GetDefaultObject();
	if (NativeFunction->HasAllFunctionFlags(FUNC_HasOutParms))
	{
		InvokeNativeFunctionOutParms(ClassDefaultObject, NativeFunction, Params, ReturnValueAddress);
	}
	else
	{
		FFrame NewStack(ClassDefaultObject, NativeFunction, Params, nullptr, NativeFunction->ChildProperties);
		NativeFunction->GetNativeFunc()(ClassDefaultObject, NewStack, ReturnValueAddress);
	}
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

	FFrame NewStack(NativeObject, NativeFunction, Params, nullptr, NativeFunction->ChildProperties);
	NativeFunction->Invoke(NativeObject, NewStack, ReturnValueAddress);
}

void UUObjectExporter::InvokeNativeFunctionOutParms(UObject* NativeObject, UFunction* NativeFunction, uint8* Params, uint8* ReturnValueAddress)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UUObjectExporter::InvokeNativeFunctionOutParms);
	
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
