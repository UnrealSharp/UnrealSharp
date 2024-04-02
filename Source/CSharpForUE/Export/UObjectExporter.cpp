#include "UObjectExporter.h"
#include "CSharpForUE/CSManager.h"
#include "CSharpForUE/TypeGenerator/CSClass.h"

void UUObjectExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(CreateNewObject)
	EXPORT_FUNCTION(GetTransientPackage)
	EXPORT_FUNCTION(NativeGetName)
	EXPORT_FUNCTION(InvokeNativeStaticFunction);
	EXPORT_FUNCTION(InvokeNativeFunction);
	EXPORT_FUNCTION(NativeIsValid)
}

void* UUObjectExporter::CreateNewObject(UObject* Outer, UClass* Class, UObject* Template)
{
	if (!IsValid(Outer))
	{
		return nullptr;
	}
	
	UObject* NewCSharpObject = NewObject<UObject>(Outer, Class, NAME_None, RF_NoFlags, Template);
	return FCSManager::Get().FindManagedObject(NewCSharpObject).GetIntPtr();
}

void* UUObjectExporter::GetTransientPackage()
{
	UPackage* TransientPackage = ::GetTransientPackage();

	if (!IsValid(TransientPackage))
	{
		return nullptr;
	}

	return FCSManager::Get().FindManagedObject(TransientPackage).GetIntPtr();
}

FName UUObjectExporter::NativeGetName(UObject* Object)
{
	if (!IsValid(Object))
	{
		return NAME_None;
	}
	
	return Object->GetFName();
}

void UUObjectExporter::InvokeNativeFunction(UObject* NativeObject, UFunction* NativeFunction, uint8* Params)
{
	FFrame NewStack(NativeObject, NativeFunction, Params, nullptr, NativeFunction->ChildProperties);
	NewStack.CurrentNativeFunction = NativeFunction;
	
	if (NativeFunction->HasAnyFunctionFlags(FUNC_HasOutParms))
	{
		FOutParmRec** LastOut = &NewStack.OutParms;
		for (TFieldIterator<FProperty> PropIt(NativeFunction); PropIt; ++PropIt)
		{
			FProperty* Property = *PropIt;
			if (Property->HasAnyPropertyFlags(CPF_OutParm))
			{
				FOutParmRec* Out = (FOutParmRec*) UE_VSTACK_ALLOC(VirtualStackAllocator, sizeof(FOutParmRec));
				
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
		}
		
		if (*LastOut)
		{
			(*LastOut)->NextOutParm = nullptr;
		}
	}

	uint8* ReturnValueAddress = nullptr;
	if (NativeFunction->ReturnValueOffset != MAX_uint16)
	{
		ReturnValueAddress = Params + NativeFunction->ReturnValueOffset;
	}
	
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
