// Native binder for FScopedTransaction, used by UnrealSharp.Interop.Bind_ScopedTransaction
// and UnrealSharp.UnrealEd.FScopedTransaction. Functional counterpart of PythonScriptPlugin's unreal.ScopedEditorTransaction.
// Editor only: both the managed wrapper and this binder are compiled out elsewhere.

#if WITH_EDITOR

#include "CSBindsRegistry.h"
#include "Editor.h"
#include "ScopedTransaction.h"

DECLARE_UNREALSHARP_BINDER(Bind_ScopedTransaction)
{
	void* Create(const char* Description)
	{
		// FScopedTransaction starts nothing when the editor cannot transact or a transaction is already open.
		if (!GEditor || !GEditor->CanTransact() || GIsTransacting)
		{
			return nullptr;
		}

		const FString SessionName = UTF8_TO_TCHAR(Description != nullptr ? Description : "");
		return new FScopedTransaction(FText::FromString(SessionName));
	}

	void Destroy(void* Transaction)
	{
		delete static_cast<FScopedTransaction*>(Transaction);
	}

	void Cancel(void* Transaction)
	{
		if (FScopedTransaction* ScopedTransaction = static_cast<FScopedTransaction*>(Transaction))
		{
			ScopedTransaction->Cancel();
		}
	}

	BIND_UNREALSHARP_FUNCTION(Create)
	BIND_UNREALSHARP_FUNCTION(Destroy)
	BIND_UNREALSHARP_FUNCTION(Cancel)
}

#endif // WITH_EDITOR
