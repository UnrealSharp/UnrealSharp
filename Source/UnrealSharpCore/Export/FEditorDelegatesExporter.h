#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "FEditorDelegatesExporter.generated.h"

using FPIEEvent = void(*)(bool);

UCLASS()
class UNREALSHARPCORE_API UFEditorDelegatesExporter : public UObject
{
	GENERATED_BODY()
	
public:

	UNREALSHARP_FUNCTION()
	static void BindEndPIE(FPIEEvent Delegate, FDelegateHandle* DelegateHandle);

	UNREALSHARP_FUNCTION()
	static void BindStartPIE(FPIEEvent Delegate, FDelegateHandle* DelegateHandle);

	UNREALSHARP_FUNCTION()
	static void UnbindEndPIE(FDelegateHandle DelegateHandle);

	UNREALSHARP_FUNCTION()
	static void UnbindStartPIE(FDelegateHandle DelegateHandle);
};
