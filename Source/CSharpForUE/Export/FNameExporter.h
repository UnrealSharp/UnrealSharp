#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "FNameExporter.generated.h"

UCLASS(meta = (NotGeneratorValid))
class CSHARPFORUE_API UFNameExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:

	// UFunctionsExporter interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End

private:
	
	static void NameToString(FName Name, FString& OutString);
	static void StringToName(FName* Name, const UTF16CHAR* String);
	static bool IsValid(FName Name);
	
};
