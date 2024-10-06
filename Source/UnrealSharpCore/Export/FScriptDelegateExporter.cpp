#include "FScriptDelegateExporter.h"

void UFScriptDelegateExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(BroadcastDelegate);
	EXPORT_FUNCTION(IsBound);
}

void UFScriptDelegateExporter::BroadcastDelegate(FScriptDelegate* Delegate, void* Params)
{
	Delegate->ProcessDelegate<UObject>(Params);
}

bool UFScriptDelegateExporter::IsBound(FScriptDelegate* Delegate)
{
	return Delegate->IsBound();
}
