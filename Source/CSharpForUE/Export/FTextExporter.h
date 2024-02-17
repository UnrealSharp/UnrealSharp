#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "FTextExporter.generated.h"

UCLASS(meta = (NotGeneratorValid))
class CSHARPFORUE_API UFTextExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:
	
	// UFunctions interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End

private:
	
	static const TCHAR* ToString(FText* Text);
	static void FromString(FText* Text, const char* String);
	static void FromName(FText* Text, FName Name);
	static void CreateEmptyText(FText* Text);
	
};
