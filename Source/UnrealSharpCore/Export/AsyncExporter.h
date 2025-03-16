#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "Async/Async.h"
#include "CSManagedGCHandle.h"
#include "AsyncExporter.generated.h"

UCLASS()
class UNREALSHARPCORE_API UAsyncExporter : public UObject
{
	GENERATED_BODY()

public:

	UNREALSHARP_FUNCTION()
	static void RunOnThread(UObject* WorldContextObject, ENamedThreads::Type Thread, GCHandleIntPtr DelegateHandle);
	
	UNREALSHARP_FUNCTION()
	static int GetCurrentNamedThread();
	
};
