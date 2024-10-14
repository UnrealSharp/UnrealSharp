// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "CSTestActor.generated.h"

UCLASS(Blueprintable, BlueprintType)
class ACSTestActor : public AActor
{
	GENERATED_BODY()

public:
	
	UFUNCTION(meta = (ScriptMethod), Category = "Test C#")
	bool MyScriptMethod(int32 MyInteger);

	UFUNCTION(meta = (ScriptNoExport), BlueprintCallable, Category = "Test C#")
	bool MyNonScriptMethod(int32 MyInteger);

	UFUNCTION(BlueprintCallable, Category = "Test C#")
	void MyTestFunction(TMap<FName, int> TestMap);

	// MyTestInteger is a test integer
	UPROPERTY(BlueprintReadOnly, VisibleAnywhere, Category = "Test C#")
	int32 MyTestInteger;

	// MyTestString is a test string
	UPROPERTY(BlueprintReadOnly, VisibleAnywhere, Category = "Test C#")
	FString MyTestString;

	// MyTestMap is a test map
	UPROPERTY(BlueprintReadWrite, VisibleAnywhere, Category= "Test C#")
	TMap<FName, int> MyTestMap;

	// MyTestArray is a test array
	UPROPERTY(BlueprintReadWrite, VisibleAnywhere, Category= "Test C#")
	TArray<int> MyTestArray;

	// MyReadOnlyTestMap is a read-only test map
	UPROPERTY(BlueprintReadOnly, VisibleAnywhere, Category= "Test C#")
	TMap<FName, int> MyReadOnlyTestMap;

	// MyReadOnlyTestArray is a read-only test array
	UPROPERTY(BlueprintReadOnly, VisibleAnywhere, Category= "Test C#")
	TArray<int> MyReadOnlyTestArray;
	
};
