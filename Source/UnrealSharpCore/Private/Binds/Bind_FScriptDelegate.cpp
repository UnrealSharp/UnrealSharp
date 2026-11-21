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

	void ProcessDelegate(FScriptDelegate* Delegate, void* Params)
	{
		if (Delegate->IsBound())
		{
			Delegate->ProcessDelegate<UObject>(Params);
		}
	}

	bool ContainsDelegate(FScriptDelegate* Delegate, UObject* Object, FName FunctionName)
	{
		return Delegate->GetUObject() == Object && Delegate->GetFunctionName() == FunctionName;
	}

	void ClearDelegate(FScriptDelegate* Delegate)
	{
		Delegate->Unbind();
	}

	void CopyDelegate(FScriptDelegate* Destination, const FScriptDelegate* Source)
	{
		*Destination = *Source;
	}
	
	BIND_UNREALSHARP_FUNCTION(BroadcastDelegate)
	BIND_UNREALSHARP_FUNCTION(IsBound)
	BIND_UNREALSHARP_FUNCTION(MakeDelegate)
	BIND_UNREALSHARP_FUNCTION(GetDelegateInfo)
	BIND_UNREALSHARP_FUNCTION(ProcessDelegate)
	BIND_UNREALSHARP_FUNCTION(ContainsDelegate)
	BIND_UNREALSHARP_FUNCTION(ClearDelegate)
	BIND_UNREALSHARP_FUNCTION(CopyDelegate)
}
