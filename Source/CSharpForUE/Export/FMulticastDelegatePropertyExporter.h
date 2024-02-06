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

	static void AddDelegate(FMulticastScriptDelegate* DelegateProperty, UObject* Target, const char* FunctionName);
	static void RemoveDelegate(FMulticastScriptDelegate* DelegateProperty, UObject* Target, const char* FunctionName);
	static void ClearDelegate(FMulticastScriptDelegate* DelegateProperty);
	static void BroadcastDelegate(FMulticastScriptDelegate* DelegateProperty, void* Parameters);
	static void ToString(FMulticastScriptDelegate* DelegateProperty, FString& OutString);
	static bool ContainsDelegate(FMulticastScriptDelegate* DelegateProperty, UObject* Target, const char* FunctionName);

	static void* GetSignatureFunction(FMulticastDelegateProperty* DelegateProperty);

	static FScriptDelegate MakeScriptDelegate(UObject* Target, const char* FunctionName);
	
};
