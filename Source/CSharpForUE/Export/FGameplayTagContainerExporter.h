// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "GameplayTagContainer.h"
#include "FGameplayTagContainerExporter.generated.h"

UCLASS(meta=(NotGeneratorValid))
class CSHARPFORUE_API UFGameplayTagContainerExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:

	// UFunctionsExporter interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End

private:

	static bool HasTag(const FGameplayTagContainer* Container, const FGameplayTag* Tag);
	static bool HasTagExact(const FGameplayTagContainer* Container, const FGameplayTag* Tag);
	static bool HasAny(const FGameplayTagContainer* Container, const FGameplayTagContainer* OtherContainer);
	static bool HasAnyExact(const FGameplayTagContainer* Container, const FGameplayTagContainer* OtherContainer);
	static bool HasAll(const FGameplayTagContainer* Container, const FGameplayTagContainer* OtherContainer);
	static bool HasAllExact(const FGameplayTagContainer* Container, const FGameplayTagContainer* OtherContainer);
	static FGameplayTagContainer Filter(const FGameplayTagContainer* Container, const FGameplayTagContainer* OtherContainer);
	static FGameplayTagContainer FilterExact(const FGameplayTagContainer* Container, const FGameplayTagContainer* OtherContainer);
	static void AppendTags(FGameplayTagContainer* Container, const FGameplayTagContainer* OtherContainer);
	static void AddTag(FGameplayTagContainer* Container, const FGameplayTag* Tag);
	static void AddTagFast(FGameplayTagContainer* Container, const FGameplayTag* Tag);
	static bool AddLeafTag(FGameplayTagContainer* Container, const FGameplayTag* Tag);
	static void RemoveTag(FGameplayTagContainer* Container, const FGameplayTag* Tag, bool bDeferParentTags);
	static void RemoveTags(FGameplayTagContainer* Container, const FGameplayTagContainer* OtherContainer);
	static void Reset(FGameplayTagContainer* Container);
	static void ToString(const FGameplayTagContainer* Container, FString& String);
	
};
