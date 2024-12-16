#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "UDataTableExporter.generated.h"

UCLASS(meta = (NotGeneratorValid))
class UNREALSHARPCORE_API UUDataTableExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:

	// UFunctionsExporter interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End

private:

	static uint8* GetRow(const UDataTable* DataTable, FName RowName);
	
};
