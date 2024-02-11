#include "FMulticastDelegatePropertyExporter.h"

void UFMulticastDelegatePropertyExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(AddDelegate)
	EXPORT_FUNCTION(RemoveDelegate)
	EXPORT_FUNCTION(ClearDelegate)
	EXPORT_FUNCTION(BroadcastDelegate)
	EXPORT_FUNCTION(GetSignatureFunction)
	EXPORT_FUNCTION(ContainsDelegate)
}

void UFMulticastDelegatePropertyExporter::AddDelegate(FMulticastDelegateProperty* DelegateProperty, FMulticastScriptDelegate* Delegate, UObject* Target, const char* FunctionName)
{
	FScriptDelegate NewScriptDelegate = MakeScriptDelegate(Target, FunctionName);
	DelegateProperty->AddDelegate(NewScriptDelegate, nullptr, Delegate);
}

void UFMulticastDelegatePropertyExporter::RemoveDelegate(FMulticastDelegateProperty* DelegateProperty, FMulticastScriptDelegate* Delegate, UObject* Target, const char* FunctionName)
{
	FScriptDelegate NewScriptDelegate = MakeScriptDelegate(Target, FunctionName);
	DelegateProperty->RemoveDelegate(NewScriptDelegate, nullptr, Delegate);
}

void UFMulticastDelegatePropertyExporter::ClearDelegate(FMulticastDelegateProperty* DelegateProperty, FMulticastScriptDelegate* Delegate)
{
	DelegateProperty->ClearDelegate(nullptr, Delegate);
}

void UFMulticastDelegatePropertyExporter::BroadcastDelegate(FMulticastDelegateProperty* DelegateProperty, const FMulticastScriptDelegate* Delegate, void* Parameters)
{
	Delegate = TryGetSparseMulticastDelegate(DelegateProperty, Delegate);
	Delegate->ProcessMulticastDelegate<UObject>(Parameters);
}

bool UFMulticastDelegatePropertyExporter::ContainsDelegate(FMulticastDelegateProperty* DelegateProperty, const FMulticastScriptDelegate* Delegate, UObject* Target, const char* FunctionName)
{
	FScriptDelegate NewScriptDelegate = MakeScriptDelegate(Target, FunctionName);
	Delegate = TryGetSparseMulticastDelegate(DelegateProperty, Delegate);
	return Delegate->Contains(NewScriptDelegate);
}

void* UFMulticastDelegatePropertyExporter::GetSignatureFunction(FMulticastDelegateProperty* DelegateProperty)
{
	return DelegateProperty->SignatureFunction;
}

FScriptDelegate UFMulticastDelegatePropertyExporter::MakeScriptDelegate(UObject* Target, const char* FunctionName)
{
	FScriptDelegate NewDelegate;
	NewDelegate.BindUFunction(Target, FunctionName);
	return NewDelegate;
}

const FMulticastScriptDelegate* UFMulticastDelegatePropertyExporter::TryGetSparseMulticastDelegate(FMulticastDelegateProperty* DelegateProperty, const FMulticastScriptDelegate* Delegate)
{
	// If the delegate is a sparse delegate, we need to get the multicast delegate from FSparseDelegate wrapper.
	if (DelegateProperty->IsA<FMulticastSparseDelegateProperty>())
	{
		Delegate = DelegateProperty->GetMulticastDelegate(Delegate);
	}

	return Delegate;
}
