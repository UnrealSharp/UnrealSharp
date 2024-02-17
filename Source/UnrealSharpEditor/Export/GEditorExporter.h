#pragma once
#include "CSharpForUE/Export/FunctionsExporter.h"

#include "CoreMinimal.h"
#include "GEditorExporter.generated.h"

UCLASS(meta = (NotGeneratorValid))
class UNREALSHARPEDITOR_API UGEditorExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:

	// UFunctionsExporter interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End

private:

	static void* GetEditorSubsystem(UClass* SubsystemClass);
};
