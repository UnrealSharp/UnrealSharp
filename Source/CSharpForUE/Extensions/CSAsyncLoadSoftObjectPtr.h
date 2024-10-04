// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "Kismet/BlueprintAsyncActionBase.h"
#include "CSAsyncLoadSoftObjectPtr.generated.h"

DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FOnSoftObjectLoaded, UObject*, Object);
DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FOnSoftObjectListLoaded, const TArray<UObject*>&, Object);

DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FOnSoftClassLoaded, TSubclassOf<UObject>, Class);
DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FOnSoftClassListLoaded, const TArray<TSubclassOf<UObject>>&, Classes);

UCLASS()
class UCSAsyncLoadSoftPtr : public UBlueprintAsyncActionBase
{
	GENERATED_BODY()

public:

	// UBlueprintAsyncActionBase interface
	virtual void Activate() override;
	//~UBlueprintAsyncActionBase interface

protected:

	TArray<FSoftObjectPath> SoftObjectPtrs;

	virtual void OnAsyncLoadComplete()
	{
		SetReadyToDestroy();
	}
};

UCLASS()
class CSHARPFORUE_API UCSAsyncLoadSoftObjectPtr : public UCSAsyncLoadSoftPtr
{
	GENERATED_BODY()

public:

	UFUNCTION(meta = (ScriptMethod))
	static UCSAsyncLoadSoftObjectPtr* AsyncLoadSoftObjectPtr(const TSoftObjectPtr<UObject>& SoftObjectPtr);
	
	UPROPERTY(BlueprintAssignable)
	FOnSoftObjectLoaded OnSuccess;

protected:

	// UCSAsyncLoadSoftPtr interface
	virtual void OnAsyncLoadComplete() override;
	//~UCSAsyncLoadSoftPtr interface
	
};

UCLASS()
class CSHARPFORUE_API UCSAsyncLoadSoftObjectPtrList : public UCSAsyncLoadSoftPtr
{
	GENERATED_BODY()

public:

	UFUNCTION(meta = (ScriptMethod))
	static UCSAsyncLoadSoftObjectPtrList* AsyncLoadSoftObjectPtrList(const TArray<TSoftObjectPtr<UObject>>& SoftObjectPtr);

	UPROPERTY(BlueprintAssignable)
	FOnSoftObjectListLoaded OnSuccess;

protected:

	// UCSAsyncLoadSoftPtr interface
	virtual void OnAsyncLoadComplete() override;
	//~UCSAsyncLoadSoftPtr interface
};

UCLASS()
class CSHARPFORUE_API UCSAsyncLoadSoftClassPtr : public UCSAsyncLoadSoftPtr
{
	GENERATED_BODY()

public:

	UFUNCTION(meta = (ScriptMethod))
	static UCSAsyncLoadSoftClassPtr* AsyncLoadSoftClassPtr(const TSoftClassPtr<UObject>& SoftObjectPtr);

	UPROPERTY(BlueprintAssignable)
	FOnSoftClassLoaded OnSuccess;

protected:

	// UCSAsyncLoadSoftPtr interface
	virtual void OnAsyncLoadComplete() override;
	//~UCSAsyncLoadSoftPtr interface
	
};

UCLASS()
class CSHARPFORUE_API UCSAsyncLoadSoftClassPtrList : public UCSAsyncLoadSoftPtr
{
	GENERATED_BODY()

public:

	UFUNCTION(meta= (ScriptMethod))
	static UCSAsyncLoadSoftClassPtrList* AsyncLoadSoftClassPtrList(const TArray<TSoftClassPtr<UObject>>& SoftObjectPtr);

	UPROPERTY(BlueprintAssignable)
	FOnSoftClassListLoaded OnSuccess;

protected:

	// UCSAsyncLoadSoftPtr interface
	virtual void OnAsyncLoadComplete() override;
	//~UCSAsyncLoadSoftPtr interface
	
};





