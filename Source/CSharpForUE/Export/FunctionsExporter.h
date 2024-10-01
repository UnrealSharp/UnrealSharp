#pragma once

#include "CoreMinimal.h"
#include "UObject/Object.h"
#include "FunctionsExporter.generated.h"

using FRegisterExportedFunction = void(*)(void*, const TCHAR*);

#define EXPORT_FUNCTION(FunctionName) RegisterExportedFunction((void*)&FunctionName, *(GetClass()->GetName() + "." + #FunctionName));

UCLASS(Abstract, NotBlueprintable, NotBlueprintType, meta = (NotGeneratorValid))
class CSHARPFORUE_API UFunctionsExporter : public UObject
{
	GENERATED_BODY()

public:

	// UFunctionsExporter interface begin
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) { PURE_VIRTUAL() }
	// End
	
	static void StartExportingAPI(FRegisterExportedFunction RegisterExportedFunction);
	
};
