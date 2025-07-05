// Fill out your copyright notice in the Description page of Project Settings.


#include "CSOptionalPropertyGenerator.h"

#include "TypeGenerator/Factories/CSPropertyFactory.h"
#include "TypeGenerator/Register/MetaData/CSContainerBaseMetaData.h"

struct FCSContainerBaseMetaData;

FProperty* UCSOptionalPropertyGenerator::CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData)
{
	FOptionalProperty* NewProperty = static_cast<FOptionalProperty*>(Super::CreateProperty(Outer, PropertyMetaData));
	TSharedPtr<FCSContainerBaseMetaData> OptionalPropertyMetaData = PropertyMetaData.GetTypeMetaData<FCSContainerBaseMetaData>();
	NewProperty->SetValueProperty(FCSPropertyFactory::CreateProperty(Outer, OptionalPropertyMetaData->InnerProperty));
	NewProperty->GetValueProperty()->Owner = NewProperty;
	return NewProperty;
}

TSharedPtr<FCSUnrealType> UCSOptionalPropertyGenerator::CreateTypeMetaData(ECSPropertyType PropertyType)
{
	return MakeShared<FCSContainerBaseMetaData>();
}
