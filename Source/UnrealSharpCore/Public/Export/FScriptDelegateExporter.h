#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "FScriptDelegateExporter.generated.h"

UCLASS()
class UFScriptDelegateExporter : public UObject
{
	GENERATED_BODY()

public:

	UNREALSHARP_FUNCTION()
	static void BroadcastDelegate(UObject* Object, FName FunctionName, void* Params);

	UNREALSHARP_FUNCTION()
	static bool IsBound(FScriptDelegate* Delegate);
	
	UNREALSHARP_FUNCTION()
	static void MakeDelegate(FScriptDelegate* OutDelegate, UObject* Object, FName FunctionName);
	
	UNREALSHARP_FUNCTION()	
	static void GetDelegateInfo(FScriptDelegate* Delegate, UObject** OutObject, FName* OutFunctionName);
	
};
