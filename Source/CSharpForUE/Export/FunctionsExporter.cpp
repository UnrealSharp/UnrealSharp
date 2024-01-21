#include "FunctionsExporter.h"
#include "UObject/UObjectIterator.h"
#include "TimerManager.h"

void UFunctionsExporter::StartExportingAPI(FRegisterExportedFunction RegisterExportedFunction)
{
	// CDOs hasn't been created yet. We need to look through the classes instead.
	for (TObjectIterator<UClass> It; It; ++It)
	{
		UClass* ClassObject = *It;
		
		if (!ClassObject->IsChildOf(StaticClass()) || ClassObject->HasAnyClassFlags(CLASS_Abstract))
		{
			continue;
		}
		
		UFunctionsExporter* FunctionsExporter = ClassObject->GetDefaultObject<UFunctionsExporter>();
		FunctionsExporter->ExportFunctions(RegisterExportedFunction);
	}
}
