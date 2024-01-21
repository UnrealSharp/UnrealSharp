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

UCLASS()
class CSHARPFORUE_API UFMulticastDelegatePropertyExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:

	// UFunctionsExporter interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End

private:

	static void AddDelegate(FMulticastScriptDelegate* DelegateProperty, UObject* Object, UObject* Target, const char* FunctionName);
	static void RemoveDelegate(FMulticastScriptDelegate* DelegateProperty, UObject* Object, UObject* Target, const char* FunctionName);
	static void ClearDelegate(FMulticastScriptDelegate* DelegateProperty, UObject* Object);
	static void BroadcastDelegate(FMulticastScriptDelegate* DelegateProperty, UObject* Object, void* Parameters);

	static void* GetSignatureFunction(FMulticastDelegateProperty* DelegateProperty);

	static FScriptDelegate MakeScriptDelegate(UObject* Target, const char* FunctionName);
	
};
