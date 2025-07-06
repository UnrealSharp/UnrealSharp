#pragma once

#include "CoreMinimal.h"
#include "Utils/CSMacros.h"
#include "CSInterface.generated.h"

UCLASS(MinimalAPI)
class UCSInterface : public UClass
{
	GENERATED_BODY()
	DECLARE_CSHARP_TYPE_FUNCTIONS(FCSInterfaceInfo)
};
