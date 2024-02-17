// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "FBoolPropertyExporter.generated.h"

UCLASS(meta = (NotGeneratorValid))
class CSHARPFORUE_API UFBoolPropertyExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:

	// UFunctionsExporter interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End

private:

	static bool GetBitfieldValueFromProperty(uint8* NativeBuffer, FProperty* Property, int32 Offset);
	static void SetBitfieldValueForProperty(uint8* NativeObject, FProperty* Property, int32 Offset, bool Value);
	
};
