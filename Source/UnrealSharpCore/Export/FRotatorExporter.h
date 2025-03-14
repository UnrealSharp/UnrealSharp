﻿#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "FRotatorExporter.generated.h"

UCLASS(meta = (NotGeneratorValid))
class UNREALSHARPCORE_API UFRotatorExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:

	// UFunctionsExporter interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End

private:
	
	static void FromMatrix(FRotator& Rotator, const FMatrix& Matrix);
	
};
