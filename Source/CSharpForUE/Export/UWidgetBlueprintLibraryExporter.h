#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "UWidgetBlueprintLibraryExporter.generated.h"

UCLASS(meta = (NotGeneratorValid))
class CSHARPFORUE_API UUWidgetBlueprintLibraryExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:

	// UFunctionsExporter interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End

private:

	static void* CreateWidget(UObject* WorldContextObject, UClass* WidgetClass, APlayerController* OwningPlayer);;
};
