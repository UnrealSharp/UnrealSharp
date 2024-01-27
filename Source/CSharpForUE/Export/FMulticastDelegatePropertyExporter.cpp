#include "FMulticastDelegatePropertyExporter.h"

void UFMulticastDelegatePropertyExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(AddDelegate)
	EXPORT_FUNCTION(RemoveDelegate)
	EXPORT_FUNCTION(ClearDelegate)
	EXPORT_FUNCTION(BroadcastDelegate)
	EXPORT_FUNCTION(GetSignatureFunction)
}

void UFMulticastDelegatePropertyExporter::AddDelegate(FMulticastScriptDelegate* DelegateProperty, UObject* Target, const char* FunctionName)
{
	if (!DelegateProperty)
	{
		return;
	}

	FScriptDelegate NewScriptDelegate = MakeScriptDelegate(Target, FunctionName);
	DelegateProperty->Add(NewScriptDelegate);
}

void UFMulticastDelegatePropertyExporter::RemoveDelegate(FMulticastScriptDelegate* DelegateProperty, UObject* Target, const char* FunctionName)
{
	if (!DelegateProperty)
	{
		return;
	}

	FScriptDelegate NewScriptDelegate = MakeScriptDelegate(Target, FunctionName);
	DelegateProperty->Remove(NewScriptDelegate);
}

void UFMulticastDelegatePropertyExporter::ClearDelegate(FMulticastScriptDelegate* DelegateProperty, UObject* Object)
{
	if (!IsValid(Object) || !DelegateProperty)
	{
		return;
	}
	
	DelegateProperty->Clear();
}

void UFMulticastDelegatePropertyExporter::BroadcastDelegate(FMulticastScriptDelegate* DelegateProperty, void* Parameters)
{
	if (DelegateProperty)
	{
		return;
	}
	
	DelegateProperty->ProcessMulticastDelegate<UObject>(Parameters);
}

void* UFMulticastDelegatePropertyExporter::GetSignatureFunction(FMulticastDelegateProperty* DelegateProperty)
{
	if (!DelegateProperty)
	{
		return nullptr;
	}
	
	return DelegateProperty->SignatureFunction;
}

FScriptDelegate UFMulticastDelegatePropertyExporter::MakeScriptDelegate(UObject* Target, const char* FunctionName)
{
	FScriptDelegate NewDelegate;
	NewDelegate.BindUFunction(Target, FunctionName);
	return NewDelegate;
}
