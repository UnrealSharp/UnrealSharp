﻿#include "UObjectExporter.h"
#include "UnrealSharpCore/CSManager.h"

void* UUObjectExporter::CreateNewObject(UObject* Outer, UClass* Class, UObject* Template)
{
	if (!IsValid(Outer))
	{
		return nullptr;
	}
	
	UObject* NewCSharpObject = NewObject<UObject>(Outer, Class, NAME_None, RF_NoFlags, Template);
	return UCSManager::Get().FindManagedObject(NewCSharpObject).GetPointer();
}

void* UUObjectExporter::GetTransientPackage()
{
	UPackage* TransientPackage = ::GetTransientPackage();

	if (!IsValid(TransientPackage))
	{
		return nullptr;
	}

	return UCSManager::Get().FindManagedObject(TransientPackage).GetPointer();
}

void UUObjectExporter::NativeGetName(UObject* Object, FName& OutName)
{
	OutName = !IsValid(Object) ? NAME_None : Object->GetFName();
}

void UUObjectExporter::InvokeNativeFunction(UObject* NativeObject, UFunction* NativeFunction, uint8* Params)
{
	FFrame NewStack(NativeObject, NativeFunction, Params, nullptr, NativeFunction->ChildProperties);
	NewStack.CurrentNativeFunction = NativeFunction;
	
	if (NativeFunction->HasAllFunctionFlags(FUNC_Net))
	{
		int32 FunctionCallspace = NativeObject->GetFunctionCallspace(NativeFunction, nullptr);
		if ((FunctionCallspace & FunctionCallspace::Remote) == 0)
		{
			return;
		}

		NativeObject->CallRemoteFunction(NativeFunction, Params, nullptr, nullptr);
		if ((FunctionCallspace & FunctionCallspace::Local) == 0)
		{
			return;
		}
	}
	
	if (NativeFunction->HasAnyFunctionFlags(FUNC_HasOutParms))
	{
		FOutParmRec** LastOut = &NewStack.OutParms;
		for (TFieldIterator<FProperty> PropIt(NativeFunction); PropIt; ++PropIt)
		{
			FProperty* Property = *PropIt;
			
			if (!Property->HasAnyPropertyFlags(CPF_OutParm))
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
	}

	const bool bHasReturnParam = NativeFunction->ReturnValueOffset != MAX_uint16;
	uint8* ReturnValueAddress = bHasReturnParam ? Params + NativeFunction->ReturnValueOffset : nullptr;
	NativeFunction->Invoke(NativeObject, NewStack, ReturnValueAddress);
}

void UUObjectExporter::InvokeNativeStaticFunction(const UClass* NativeClass, UFunction* NativeFunction, uint8* Params)
{
	InvokeNativeFunction(NativeClass->ClassDefaultObject, NativeFunction, Params);
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
	return UCSManager::Get().FindManagedObject(World).GetPointer();
}

uint32 UUObjectExporter::GetUniqueID(UObject* Object)
{
	return Object->GetUniqueID();
}