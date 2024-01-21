#include "UObjectExporter.h"
#include "CSharpForUE/CSManager.h"

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
	if (!IsValid(NativeObject) || !IsValid(NativeFunction))
	{
		return;
	}

	if (NativeFunction->HasAnyFunctionFlags(FUNC_HasOutParms))
	{
		for (TFieldIterator<FProperty> PropIt(NativeFunction); PropIt; ++PropIt)
		{
			FProperty* Prop = *PropIt;

			if (!Prop->HasAnyPropertyFlags(CPF_Parm | CPF_OutParm) || Prop->HasAnyPropertyFlags(CPF_ReturnParm))
			{
				continue;
			}

			if (!Prop->HasAnyPropertyFlags(CPF_ReferenceParm))
			{
				// TODO: Initialize value in C# instead of here
				Prop->InitializeValue_InContainer(Params);
			}
		}
	}
	
	NativeObject->ProcessEvent(NativeFunction, Params);
}

void UUObjectExporter::InvokeNativeStaticFunction(const UClass* NativeClass, UFunction* NativeFunction, uint8* Params)
{
	InvokeNativeFunction(NativeClass->ClassDefaultObject, NativeFunction, Params);
} 

bool UUObjectExporter::NativeIsValid(UObject* Object)
{
	return IsValid(Object);
}
