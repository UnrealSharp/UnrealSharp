#include "CSBindsRegistry.h"

DECLARE_UNREALSHARP_BINDER(Bind_FScriptDelegate)
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
	
	BIND_UNREALSHARP_FUNCTION(BroadcastDelegate)
	BIND_UNREALSHARP_FUNCTION(IsBound)
	BIND_UNREALSHARP_FUNCTION(MakeDelegate)
	BIND_UNREALSHARP_FUNCTION(GetDelegateInfo)
}
