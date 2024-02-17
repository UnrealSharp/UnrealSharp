#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "FMatrixExporter.generated.h"

UCLASS(meta = (NotGeneratorValid))
class CSHARPFORUE_API UFMatrixExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:

	// UFunctionsExporter interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End

private:
	
	static void FromRotator(FMatrix& Matrix, const FRotator& Rotator);
	
};
