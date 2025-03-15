#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "CSTimerExtensions.generated.h"

using FNextTickEvent = void(*)();

UCLASS()
class UNREALSHARPCORE_API UCSTimerExtensions : public UObject
{
	GENERATED_BODY()
	
public:

	UNREALSHARP_FUNCTION()
	static void SetTimerForNextTick(FNextTickEvent NextTickEvent);
};
