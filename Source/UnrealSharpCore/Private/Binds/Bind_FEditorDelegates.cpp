

#if WITH_EDITOR
#include "Editor.h"
#endif

#include "CSBindsRegistry.h"

using FPIEEvent = void(*)(bool);

DECLARE_UNREALSHARP_BINDER(Bind_FEditorDelegates)
{
	void BindEndPIE(FPIEEvent Delegate, FDelegateHandle* DelegateHandle)
	{
#if WITH_EDITOR
		*DelegateHandle = FEditorDelegates::EndPIE.AddLambda(Delegate);
#endif
	}

	void BindStartPIE(FPIEEvent Delegate, FDelegateHandle* DelegateHandle)
	{
#if WITH_EDITOR
		*DelegateHandle = FEditorDelegates::BeginPIE.AddLambda(Delegate);
#endif
	}

	void UnbindStartPIE(FDelegateHandle DelegateHandle)
	{
#if WITH_EDITOR
		FEditorDelegates::BeginPIE.Remove(DelegateHandle);
#endif
	}

	void UnbindEndPIE(FDelegateHandle DelegateHandle)
	{
#if WITH_EDITOR
		FEditorDelegates::EndPIE.Remove(DelegateHandle);
#endif
	}
	
	BIND_UNREALSHARP_FUNCTION(BindEndPIE)
	BIND_UNREALSHARP_FUNCTION(BindStartPIE)
	BIND_UNREALSHARP_FUNCTION(UnbindStartPIE)
	BIND_UNREALSHARP_FUNCTION(UnbindEndPIE)
}

