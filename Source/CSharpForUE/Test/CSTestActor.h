// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "CSTestActor.generated.h"

UCLASS(Blueprintable, BlueprintType)
class CSHARPFORUE_API ACSTestActor : public AActor
{
	GENERATED_BODY()

public:
	
	UFUNCTION(meta = (ScriptMethod), Category = "Test C#")
	bool MyScriptMethod(int32 MyInteger);

	UFUNCTION(meta = (ScriptNoExport), BlueprintCallable, Category = "Test C#")
	bool MyNonScriptMethod(int32 MyInteger);

	UPROPERTY(BlueprintReadOnly, Category = "Test C#")
	int32 MyTestInteger;
	
};
