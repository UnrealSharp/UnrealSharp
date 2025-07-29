// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "CSScriptBuilder.h"
#include "UObject/Object.h"
#include "CSGlueGenerator.generated.h"

UCLASS(NotBlueprintable, NotBlueprintType)
class UNREALSHARPRUNTIMEGLUE_API UCSGlueGenerator : public UObject
{
	GENERATED_BODY()
public:
	virtual void Initialize() {}
	virtual void ForceRefresh() {}
protected:
	void SaveRuntimeGlue(const FCSScriptBuilder& ScriptBuilder, const FString& FileName, const FString& Suffix = FString(TEXT(".cs")));
};
