#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "AActorExporter.generated.h"

UCLASS(meta = (NotGeneratorValid))
class CSHARPFORUE_API UAActorExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:
	
	// UFunctions interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End

private:
	
	
};
