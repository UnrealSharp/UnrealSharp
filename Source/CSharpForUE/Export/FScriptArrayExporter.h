#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "FScriptArrayExporter.generated.h"

UCLASS(meta=(NotGeneratorValid))
class CSHARPFORUE_API UFScriptArrayExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:

	// Begin UFunctionsExporter interface
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End UFunctionsExporter interface

private:

	static void* GetData(FScriptArray* Instance);
	static bool IsValidIndex(FScriptArray* Instance, int32 i);
	static int Num(FScriptArray* Instance);
	static void Destroy(FScriptArray* Instance);
	
};
