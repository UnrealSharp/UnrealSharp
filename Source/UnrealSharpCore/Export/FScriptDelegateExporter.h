#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "FScriptDelegateExporter.generated.h"

UCLASS()
class UNREALSHARPCORE_API UFScriptDelegateExporter : public UObject
{
	GENERATED_BODY()

public:

	UNREALSHARP_FUNCTION()
	static void BroadcastDelegate(FScriptDelegate* Delegate, void* Params);

	UNREALSHARP_FUNCTION()
	static bool IsBound(FScriptDelegate* Delegate);
	
};
