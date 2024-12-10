#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "FMsgExporter.generated.h"

UCLASS(meta = (NotGeneratorValid))
class UNREALSHARPCORE_API UFMsgExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:
	
	// UFunctions interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End

private:
	
	static void Log(FName CategoryName, ELogVerbosity::Type Verbosity, const UTF16CHAR* Message);
	
};
