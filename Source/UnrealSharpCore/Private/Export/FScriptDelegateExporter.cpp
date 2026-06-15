#include "CSBindsManager.h"

DECLARE_UNREALSHARP_EXPORTER(FScriptDelegateExporter)
{
	void BroadcastDelegate(UObject* Object, FName FunctionName, void* Params)
	{
		FScriptDelegate Delegate;
		Delegate.BindUFunction(Object, FunctionName);
		Delegate.ProcessDelegate<UObject>(Params);
	}

	bool IsBound(FScriptDelegate* Delegate)
	{
		return Delegate->IsBound();
	}

	void MakeDelegate(FScriptDelegate* OutDelegate, UObject* Object, FName FunctionName)
	{
		OutDelegate->BindUFunction(Object, FunctionName);
	}

	void GetDelegateInfo(FScriptDelegate* Delegate, UObject** OutObject, FName* OutFunctionName)
	{
		*OutObject = Delegate->GetUObject();
		*OutFunctionName = Delegate->GetFunctionName();
	}
	
	EXPORT_UNREALSHARP_FUNCTION(BroadcastDelegate)
	EXPORT_UNREALSHARP_FUNCTION(IsBound)
	EXPORT_UNREALSHARP_FUNCTION(MakeDelegate)
	EXPORT_UNREALSHARP_FUNCTION(GetDelegateInfo)
}
