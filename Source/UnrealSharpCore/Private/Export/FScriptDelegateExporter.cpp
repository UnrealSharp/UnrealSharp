#include "Export/FScriptDelegateExporter.h"

void UFScriptDelegateExporter::BroadcastDelegate(UObject* Object, FName FunctionName, void* Params)
{
	FScriptDelegate Delegate;
	Delegate.BindUFunction(Object, FunctionName);
	Delegate.ProcessDelegate<UObject>(Params);
}

bool UFScriptDelegateExporter::IsBound(FScriptDelegate* Delegate)
{
	return Delegate->IsBound();
}

void UFScriptDelegateExporter::MakeDelegate(FScriptDelegate* OutDelegate, UObject* Object, FName FunctionName)
{
    OutDelegate->BindUFunction(Object, FunctionName);
}

void UFScriptDelegateExporter::GetDelegateInfo(FScriptDelegate* Delegate, UObject** OutObject, FName* OutFunctionName)
{
	*OutObject = Delegate->GetUObject();
    *OutFunctionName = Delegate->GetFunctionName();
}
