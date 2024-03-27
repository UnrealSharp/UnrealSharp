#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "FStringExporter.generated.h"

UCLASS()
class CSHARPFORUE_API UFStringExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:
	
	// UFunctionsExporter interface
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End of UFunctionsExporter interface

private:

	static void MarshalToNativeString(FString* String, TCHAR* ManagedString);
	
};
