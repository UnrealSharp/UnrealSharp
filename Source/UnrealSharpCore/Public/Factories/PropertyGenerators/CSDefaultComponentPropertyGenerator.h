// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "CSPropertyGenerator.h"
#include "CSDefaultComponentPropertyGenerator.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSDefaultComponentPropertyGenerator : public UCSPropertyGenerator
{
	GENERATED_BODY()
public:
	// UCSPropertyGenerator interface
	virtual ECSPropertyType GetPropertyType() const override { return ECSPropertyType::DefaultComponent; }
	virtual TSharedPtr<FCSUnrealType> CreateTypeMetaData(ECSPropertyType PropertyType) override;
	virtual FProperty* CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData) override;
	// End of implementation
};
