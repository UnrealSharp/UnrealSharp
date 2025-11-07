#include "Export/FScriptDelegateExporter.h"

void UFScriptDelegateExporter::BroadcastDelegate(FScriptDelegate* Delegate, void* Params)
{
	Delegate->ProcessDelegate<UObject>(Params);
}

bool UFScriptDelegateExporter::IsBound(FScriptDelegate* Delegate)
{
	return Delegate->IsBound();
}
