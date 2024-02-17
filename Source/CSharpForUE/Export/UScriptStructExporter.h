#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "UScriptStructExporter.generated.h"

UCLASS(meta = (NotGeneratorValid))
class CSHARPFORUE_API UUScriptStructExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:

	// UFunctionsExporter interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End

private:

	static int GetNativeStructSize(const UScriptStruct* ScriptStruct);
	
};
