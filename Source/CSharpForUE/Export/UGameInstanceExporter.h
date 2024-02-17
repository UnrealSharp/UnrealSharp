#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "UGameInstanceExporter.generated.h"

UCLASS(meta = (NotGeneratorValid))
class CSHARPFORUE_API UUGameInstanceExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:

	// UFunctionsExporter interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End

private:

	static void* GetGameInstanceSubsystem(UClass* SubsystemClass, UObject* WorldContextObject);
};
