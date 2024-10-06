#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "FVectorExporter.generated.h"

UCLASS(meta = (NotGeneratorValid))
class UNREALSHARPCORE_API UFVectorExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:

	// UFunctions interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End

private:

	static FVector FromRotator(const FRotator& Rotator);
	
};
