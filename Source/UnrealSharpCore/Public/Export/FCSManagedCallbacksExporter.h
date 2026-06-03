#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "CSManagedCallbacksCache.h"
#include "UObject/Object.h"
#include "FCSManagedCallbacksExporter.generated.h"

struct FCSManagedCallbacks;

UCLASS()
class UFCSManagedCallbacksExporter : public UObject
{
	GENERATED_BODY()
public:
	UNREALSHARP_FUNCTION()
	static FCSManagedCallbacks* GetManagedCallbacks();
};
