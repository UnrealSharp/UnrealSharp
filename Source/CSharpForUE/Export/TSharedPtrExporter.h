#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "TSharedPtrExporter.generated.h"

UCLASS(meta = (NotGeneratorValid))
class CSHARPFORUE_API UTSharedPtrExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:

	// UFunctionsExporter interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End

private:

	static void AddSharedReference(SharedPointerInternals::TReferenceControllerBase<ESPMode::ThreadSafe>* ReferenceController);
	static void ReleaseSharedReference(SharedPointerInternals::TReferenceControllerBase<ESPMode::ThreadSafe>* ReferenceController);
	
};
