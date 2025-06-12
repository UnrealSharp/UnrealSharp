// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
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
class UNREALSHARPCORE_API UFMulticastDelegatePropertyExporter : public UObject
{
	GENERATED_BODY()

public:
	UNREALSHARP_FUNCTION()
	static void AddDelegate(FMulticastDelegateProperty* DelegateProperty, FMulticastScriptDelegate* Delegate, UObject* Target, const char* FunctionName);

	UNREALSHARP_FUNCTION()
	static bool IsBound(FMulticastScriptDelegate* Delegate);

	UNREALSHARP_FUNCTION()
	static void ToString(FMulticastScriptDelegate* Delegate, FString* OutString);
	
	UNREALSHARP_FUNCTION()
	static void RemoveDelegate(FMulticastDelegateProperty* DelegateProperty, FMulticastScriptDelegate* Delegate, UObject* Target, const char* FunctionName);

	UNREALSHARP_FUNCTION()
	static void ClearDelegate(FMulticastDelegateProperty* DelegateProperty, FMulticastScriptDelegate* Delegate);

	UNREALSHARP_FUNCTION()
	static void BroadcastDelegate(FMulticastDelegateProperty* DelegateProperty, const FMulticastScriptDelegate* Delegate, void* Parameters);

	UNREALSHARP_FUNCTION()
	static bool ContainsDelegate(FMulticastDelegateProperty* DelegateProperty, const FMulticastScriptDelegate* Delegate, UObject* Target, const char* FunctionName);

	UNREALSHARP_FUNCTION()
	static void* GetSignatureFunction(FMulticastDelegateProperty* DelegateProperty);

	UNREALSHARP_FUNCTION()
	static FScriptDelegate MakeScriptDelegate(UObject* Target, const char* FunctionName);

	UNREALSHARP_FUNCTION()
	static const FMulticastScriptDelegate* TryGetSparseMulticastDelegate(FMulticastDelegateProperty* DelegateProperty, const FMulticastScriptDelegate* Delegate);
	
};
