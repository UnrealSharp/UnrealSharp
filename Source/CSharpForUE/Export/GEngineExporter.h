#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "GEngineExporter.generated.h"

UCLASS(meta = (NotGeneratorValid))
class CSHARPFORUE_API UGEngineExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:

	// UFunctionsExporter interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End

private:

	static void* GetEngineSubsystem(UClass* SubsystemClass);
	
};
