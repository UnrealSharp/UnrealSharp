// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "CSCommonPropertyGenerator.h"
#include "CSPropertyGenerator.h"
#include "CSDelegateBasePropertyGenerator.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSDelegateBasePropertyGenerator : public UCSCommonPropertyGenerator
{
	GENERATED_BODY()
public:
	UCSDelegateBasePropertyGenerator(FObjectInitializer const& ObjectInitializer);
protected:
	// UCSPropertyGenerator interface
	virtual FProperty* CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData) override;
	// End of UCSPropertyGenerator interface
};
