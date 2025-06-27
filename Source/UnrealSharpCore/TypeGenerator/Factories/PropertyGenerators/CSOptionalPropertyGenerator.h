// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "CSPropertyGenerator.h"
#include "UObject/PropertyOptional.h"
#include "CSOptionalPropertyGenerator.generated.h"

/**
 * 
 */
UCLASS()
class UNREALSHARPCORE_API UCSOptionalPropertyGenerator : public UCSPropertyGenerator
{
	GENERATED_BODY()

protected:
	// Begin UCSPropertyGenerator interface
	virtual ECSPropertyType GetPropertyType() const override { return ECSPropertyType::Optional; }
	virtual FFieldClass* GetPropertyClass() override { return FOptionalProperty::StaticClass(); }
	virtual FProperty* CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData) override;
	virtual TSharedPtr<FCSUnrealType> CreateTypeMetaData(ECSPropertyType PropertyType) override;
	// End UCSPropertyGenerator interface
};
