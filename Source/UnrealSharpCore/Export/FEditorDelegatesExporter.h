#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "FEditorDelegatesExporter.generated.h"

using FPIEEvent = void(*)(bool);

UCLASS(meta = (NotGeneratorValid))
class UNREALSHARPCORE_API UFEditorDelegatesExporter : public UFunctionsExporter
{
	GENERATED_BODY()
	
public:

	// UFunctionsExporter interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End

private:
	
	static void BindEndPIE(FPIEEvent Delegate, FDelegateHandle& DelegateHandle);
	static void BindStartPIE(FPIEEvent Delegate, FDelegateHandle& DelegateHandle);

	static void UnbindEndPIE(FDelegateHandle DelegateHandle);
	static void UnbindStartPIE(FDelegateHandle DelegateHandle);
};
