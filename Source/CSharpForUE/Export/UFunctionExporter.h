// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "UFunctionExporter.generated.h"

UCLASS(meta = (NotGeneratorValid))
class CSHARPFORUE_API UUFunctionExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:

	// UFunctionsExporter interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End

private:

	static uint16 GetNativeFunctionParamsSize(const UFunction* NativeFunction);
	
};
