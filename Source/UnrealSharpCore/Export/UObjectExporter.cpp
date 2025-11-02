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

	NativeObject->ProcessEvent(NativeFunction, Params);
}

void UUObjectExporter::InvokeNativeStaticFunction(UClass* NativeClass, UFunction* NativeFunction, uint8* Params, uint8* ReturnValueAddress)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UUObjectExporter::InvokeNativeStaticFunction);
	UObject* ClassDefaultObject = NativeClass->GetDefaultObject();

	ClassDefaultObject->ProcessEvent(NativeFunction, Params);
}

void UUObjectExporter::InvokeNativeNetFunction(UObject* NativeObject, UFunction* NativeFunction, uint8* Params, uint8* ReturnValueAddress)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UUObjectExporter::InvokeNativeNetFunction);
	
	NativeObject->ProcessEvent(NativeFunction, Params);
}

void UUObjectExporter::InvokeNativeFunctionOutParms(UObject* NativeObject, UFunction* NativeFunction, uint8* Params, uint8* ReturnValueAddress)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UUObjectExporter::InvokeNativeFunctionOutParms);

	NativeObject->ProcessEvent(NativeFunction, Params);
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
