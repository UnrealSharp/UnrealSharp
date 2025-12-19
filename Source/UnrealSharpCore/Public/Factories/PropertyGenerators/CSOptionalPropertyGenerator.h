// Fill out your copyright notice in the Description page of Project Settings.
#pragma once

#include "CoreMinimal.h"
#include "CSPropertyGenerator.h"
#include "UnrealSharpUtils.h"
#include "CSOptionalPropertyGenerator.generated.h"


UCLASS()
class UNREALSHARPCORE_API UCSOptionalPropertyGenerator : public UCSPropertyGenerator
{
	GENERATED_BODY()

protected:

	// Begin UCSPropertyGenerator interface
	virtual ECSPropertyType GetPropertyType() const override { return ECSPropertyType::Optional; }
#if UE_VERSION_SINCE(5, 6)
	virtual FFieldClass* GetPropertyClass() override { return FOptionalProperty::StaticClass(); }
#else
	virtual FFieldClass* GetPropertyClass() override { return nullptr; }
#endif

	virtual FProperty* CreateProperty(UField* Outer, const FCSPropertyReflectionData& PropertyReflectionData) override;
	virtual TSharedPtr<FCSUnrealType> CreatePropertyInnerTypeData(ECSPropertyType PropertyType) override;
	// End UCSPropertyGenerator interface
};
