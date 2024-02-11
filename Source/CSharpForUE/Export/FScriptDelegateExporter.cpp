#include "FScriptDelegateExporter.h"

void UFScriptDelegateExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(BroadcastDelegate);
}

void UFScriptDelegateExporter::BroadcastDelegate(FScriptDelegate* Delegate, void* Params)
{
	Delegate->ProcessDelegate<UObject>(Params);
}
