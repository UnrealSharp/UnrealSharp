// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "CSCommonPropertyGenerator.h"
#include "CSClassPropertyGenerator.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSClassPropertyGenerator : public UCSPropertyGenerator
{
	GENERATED_BODY()

protected:

	// Begin UCSPropertyGenerator interface
	virtual ECSPropertyType GetPropertyType() const override { return ECSPropertyType::Class; }
	virtual FFieldClass* GetPropertyClass() override { return FClassProperty::StaticClass(); }
	virtual FProperty* CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData) override;
	virtual TSharedPtr<FCSUnrealType> CreateTypeMetaData(ECSPropertyType PropertyType) override;
#if WITH_EDITOR
	virtual FEdGraphPinType GetPinType(ECSPropertyType PropertyType, const FCSPropertyMetaData& MetaData, UBlueprint* Outer) const override;
#endif
	// End UCSPropertyGenerator interface
};
