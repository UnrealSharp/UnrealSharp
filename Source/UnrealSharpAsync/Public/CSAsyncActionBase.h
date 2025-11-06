// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "CSManagedDelegate.h"
#include "CSManagedGCHandle.h"
#include "UnrealSharpBinds/Public/CSBindsManager.h"
#include "UObject/Object.h"
#include "CSAsyncActionBase.generated.h"

UCLASS()
class UNREALSHARPASYNC_API UCSAsyncActionBase : public UObject
{
	GENERATED_BODY()
public:
	UFUNCTION(meta = (ScriptMethod))
	void Destroy();
protected:
	friend class UUCSAsyncBaseExporter;

	void InvokeManagedCallback(bool bDispose = true);
    void InvokeManagedCallback(UObject* WorldContextObject, bool bDispose = true);
	void InitializeManagedCallback(FGCHandleIntPtr Callback);
	
	FCSManagedDelegate ManagedCallback;
};

UCLASS(meta = (InternalType))
class UUCSAsyncBaseExporter : public UObject
{
	GENERATED_BODY()
public:
	UNREALSHARP_FUNCTION()
	static void InitializeAsyncObject(UCSAsyncActionBase* AsyncAction, FGCHandleIntPtr Callback);
};
