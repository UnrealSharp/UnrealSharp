#pragma once

#include "CoreMinimal.h"
#include "CSManagedTypeInterface.h"
#include "CSInterface.generated.h"

UCLASS(MinimalAPI)
class UCSInterface : public UClass, public ICSManagedTypeInterface
{
	GENERATED_BODY()
};
