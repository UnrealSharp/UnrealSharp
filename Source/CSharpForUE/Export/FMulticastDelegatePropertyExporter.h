// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "FMulticastDelegatePropertyExporter.generated.h"

struct Interop_FScriptDelegate
{
	UObject* Object;
	FName Name;

	FScriptDelegate ToFScriptDelegate() const
	{
		FScriptDelegate NewScriptDelegate;
		NewScriptDelegate.BindUFunction(Object, Name);
		return NewScriptDelegate;
	}
};

UCLASS(meta = (NotGeneratorValid))
class CSHARPFORUE_API UFMulticastDelegatePropertyExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:

	// UFunctionsExporter interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End

private:

	static void AddDelegate(FMulticastDelegateProperty* DelegateProperty, FMulticastScriptDelegate* Delegate, UObject* Target, const char* FunctionName);
	static void RemoveDelegate(FMulticastDelegateProperty* DelegateProperty, FMulticastScriptDelegate* Delegate, UObject* Target, const char* FunctionName);
	static void ClearDelegate(FMulticastDelegateProperty* DelegateProperty, FMulticastScriptDelegate* Delegate);
	static void BroadcastDelegate(FMulticastDelegateProperty* DelegateProperty, const FMulticastScriptDelegate* Delegate, void* Parameters);
	static bool ContainsDelegate(FMulticastDelegateProperty* DelegateProperty, const FMulticastScriptDelegate* Delegate, UObject* Target, const char* FunctionName);

	static void* GetSignatureFunction(FMulticastDelegateProperty* DelegateProperty);

	static FScriptDelegate MakeScriptDelegate(UObject* Target, const char* FunctionName);
	static const FMulticastScriptDelegate* TryGetSparseMulticastDelegate(FMulticastDelegateProperty* DelegateProperty, const FMulticastScriptDelegate* Delegate);
	
};
