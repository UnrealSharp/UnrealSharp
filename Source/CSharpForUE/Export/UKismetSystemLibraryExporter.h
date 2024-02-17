#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "UKismetSystemLibraryExporter.generated.h"

UCLASS(meta = (NotGeneratorValid))
class CSHARPFORUE_API UUKismetSystemLibraryExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:

	// UFunctionsExporter interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End

private:
	
	static void PrintString(const UObject* WorldContextObject, const UTF16CHAR* Message, float Duration, FLinearColor Color, bool PrintToScreen, bool PrintToConsole);
};
