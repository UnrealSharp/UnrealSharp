// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "CSPropertyGenerator.h"
#include "CSDefaultComponentPropertyGenerator.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSDefaultComponentPropertyGenerator : public UCSPropertyGenerator
{
	GENERATED_BODY()
	
#if WITH_EDITOR
	virtual ECSPropertyType GetPropertyType() const override { return ECSPropertyType::DefaultComponent; }
	virtual void CreatePropertyEditor(UBlueprint* Blueprint, const FCSPropertyMetaData& PropertyMetaData) override;
	virtual TSharedPtr<FCSUnrealType> CreateTypeMetaData(ECSPropertyType PropertyType) override;
#endif
};
