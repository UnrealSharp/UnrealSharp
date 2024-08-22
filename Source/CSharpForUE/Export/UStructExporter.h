#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "UStructExporter.generated.h"

UCLASS(meta=(NotGeneratorValid))
class CSHARPFORUE_API UUStructExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:

	// UFunctionsExporter interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End
	
private:

	static void InitializeStruct(UStruct* Struct, void* Data);
	
};
