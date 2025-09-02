// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "Subsystems/EngineSubsystem.h"
#include "AttributeSet.h"
#include "GameplayAttributeSubsystem.generated.h"

/**
 * 
 */
UCLASS()
class UNREALSHARPCORE_API UGameplayAttributeSubsystem : public UEngineSubsystem
{
	GENERATED_BODY()

public:
	virtual void Initialize(FSubsystemCollectionBase& Collection) override;

	void GetCachedAttributeNamesForClass(const FString& AttributeSetClassName, TArray<FString>& OutAttributeNames) const;

	UFUNCTION(BlueprintCallable)
	static UGameplayAttributeSubsystem* Get();

	UFUNCTION(BlueprintCallable, meta=(ScriptMethod))
	static FGameplayAttribute FindGameplayAttributeByName(const FString& AttributeSetClassName, const FString& PropertyName);

	static void GetAllAttributeProperties(UClass* AttributeSetClass, TArray<FProperty*>& OutProperties);
	
private:
	void CacheAllGameplayAttributes();

	TMap<FString, FGameplayAttribute> CachedAttributes;
};
