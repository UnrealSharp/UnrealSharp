#include "CSBindsManager.h"
#include "UnrealSharpCore.h"

DECLARE_UNREALSHARP_EXPORTER(FMulticastDelegatePropertyExporter)
{
	struct Interop_FScriptDelegate
	{
		UObject* Object;
		FName Name;

		FScriptDelegate ToFScriptDelegate() const
		{
			FScriptDelegate NewScriptDelegate;
			NewScriptDelegate.BindUFunction(Object, Name);
			return NewScriptDelegate;
		}
	};
	
	static FScriptDelegate MakeScriptDelegate(UObject* Target, const char* FunctionName)
	{
		FScriptDelegate NewDelegate;
		NewDelegate.BindUFunction(Target, FunctionName);

		if (!NewDelegate.IsBound())
		{
			UE_LOGFMT(LogUnrealSharp, Warning, "Failed to bind function {FunctionName} on target {TargetName}", FunctionName, *Target->GetName());
		}
		
		return NewDelegate;
	}
	
	const FMulticastScriptDelegate* TryGetSparseMulticastDelegate(FMulticastDelegateProperty* DelegateProperty, const FMulticastScriptDelegate* Delegate)
	{
		// If the delegate is a sparse delegate, we need to get the multicast delegate from FSparseDelegate wrapper.
		if (DelegateProperty->IsA<FMulticastSparseDelegateProperty>())
		{
			Delegate = DelegateProperty->GetMulticastDelegate(Delegate);
		}

		return Delegate;
	}
	
	void AddDelegate(FMulticastDelegateProperty* DelegateProperty, FMulticastScriptDelegate* Delegate, UObject* Target, const char* FunctionName)
	{
		FScriptDelegate NewScriptDelegate = MakeScriptDelegate(Target, FunctionName);
		DelegateProperty->AddDelegate(NewScriptDelegate, nullptr, Delegate);
	}

	bool IsBound(FMulticastScriptDelegate* Delegate)
	{
		return Delegate->IsBound();
	}

	void ToString(FMulticastScriptDelegate* Delegate, FString* OutString)
	{
		*OutString = Delegate->ToString<UObject>();
	}

	void RemoveDelegate(FMulticastDelegateProperty* DelegateProperty, FMulticastScriptDelegate* Delegate, UObject* Target, const char* FunctionName)
	{
		FScriptDelegate NewScriptDelegate = MakeScriptDelegate(Target, FunctionName);
		DelegateProperty->RemoveDelegate(NewScriptDelegate, nullptr, Delegate);
	}

	void ClearDelegate(FMulticastDelegateProperty* DelegateProperty, FMulticastScriptDelegate* Delegate)
	{
		DelegateProperty->ClearDelegate(nullptr, Delegate);
	}

	void BroadcastDelegate(FMulticastDelegateProperty* DelegateProperty, const FMulticastScriptDelegate* Delegate, void* Parameters)
	{
		Delegate = TryGetSparseMulticastDelegate(DelegateProperty, Delegate);
		Delegate->ProcessMulticastDelegate<UObject>(Parameters);
	}

	bool ContainsDelegate(FMulticastDelegateProperty* DelegateProperty, const FMulticastScriptDelegate* Delegate, UObject* Target, const char* FunctionName)
	{
		FScriptDelegate NewScriptDelegate = MakeScriptDelegate(Target, FunctionName);
		Delegate = TryGetSparseMulticastDelegate(DelegateProperty, Delegate);
		return Delegate->Contains(NewScriptDelegate);
	}

	void* GetSignatureFunction(FMulticastDelegateProperty* DelegateProperty)
	{
		return DelegateProperty->SignatureFunction;
	}
	
	EXPORT_UNREALSHARP_FUNCTION(AddDelegate)
	EXPORT_UNREALSHARP_FUNCTION(IsBound)
	EXPORT_UNREALSHARP_FUNCTION(ToString)
	EXPORT_UNREALSHARP_FUNCTION(RemoveDelegate)
	EXPORT_UNREALSHARP_FUNCTION(ClearDelegate)
	EXPORT_UNREALSHARP_FUNCTION(BroadcastDelegate)
	EXPORT_UNREALSHARP_FUNCTION(ContainsDelegate)
	EXPORT_UNREALSHARP_FUNCTION(GetSignatureFunction)
}
