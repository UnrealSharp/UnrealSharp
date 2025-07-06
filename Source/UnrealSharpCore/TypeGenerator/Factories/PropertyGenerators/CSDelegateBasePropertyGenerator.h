// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "CSPropertyGenerator.h"
#include "CSDelegateBasePropertyGenerator.generated.h"

UCLASS(Abstract)
class UNREALSHARPCORE_API UCSDelegateBasePropertyGenerator : public UCSPropertyGenerator
{
	GENERATED_BODY()
protected:
	// UCSPropertyGenerator interface
	virtual FProperty* CreateProperty(UField* Outer, const FCSPropertyMetaData& PropertyMetaData) override;
	// End of UCSPropertyGenerator interface
};
