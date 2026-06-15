

#if WITH_EDITOR
#include "Editor.h"
#endif

#include "CSBindsManager.h"

using FPIEEvent = void(*)(bool);

DECLARE_UNREALSHARP_EXPORTER(FEditorDelegatesExporter)
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
	
	EXPORT_UNREALSHARP_FUNCTION(BindEndPIE)
	EXPORT_UNREALSHARP_FUNCTION(BindStartPIE)
	EXPORT_UNREALSHARP_FUNCTION(UnbindStartPIE)
	EXPORT_UNREALSHARP_FUNCTION(UnbindEndPIE)
}

