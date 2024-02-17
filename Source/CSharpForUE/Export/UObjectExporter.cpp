#include "UObjectExporter.h"
#include "CSharpForUE/CSManager.h"
#include "CSharpForUE/TypeGenerator/CSClass.h"

void UUObjectExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(CreateNewObject)
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
	// Initialize out parameters
	FFrame NewStack(NativeObject, NativeFunction, Params, nullptr, NativeFunction->ChildProperties);
	
	if (NativeFunction->HasAnyFunctionFlags(FUNC_HasOutParms))
	{
		for (TFieldIterator<FProperty> PropIt(NativeFunction); PropIt; ++PropIt)
		{
			FProperty* Prop = *PropIt;

			// Ignore return value
			if (!Prop->HasAllPropertyFlags(CPF_Parm | CPF_OutParm) || Prop->HasAllPropertyFlags(CPF_ReturnParm))
			{
				continue;
			}

			// Ignore reference parameters
			if (Prop->HasAllPropertyFlags(CPF_ReferenceParm))
			{
				continue;
			}

			// TODO: Initialize value in C# instead of here
			Prop->InitializeValue_InContainer(Params);

			FOutParmRec* Out = static_cast<FOutParmRec*>(FMemory_Alloca(sizeof(FOutParmRec)));
			Out->Property = Prop;
			Out->PropAddr = Params + Prop->GetOffset_ForUFunction();
			Out->NextOutParm = nullptr;
			
			if (NewStack.OutParms)
			{
				NewStack.OutParms->NextOutParm = Out;
			}
			else
			{
				NewStack.OutParms = Out;
			}
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
