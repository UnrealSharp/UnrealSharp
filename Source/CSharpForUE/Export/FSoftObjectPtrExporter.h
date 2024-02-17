#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "FSoftObjectPtrExporter.generated.h"

UCLASS(meta = (NotGeneratorValid))
class CSHARPFORUE_API UFSoftObjectPtrExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:

	// UFunctionsExporter interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End

private:
	
	static void* LoadSynchronous(const TSoftObjectPtr<UObject>& SoftObjectPtr);
	
};
