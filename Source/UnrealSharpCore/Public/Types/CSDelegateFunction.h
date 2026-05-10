#pragma once

#include "CoreMinimal.h"
#include "CSManagedTypeInterface.h"
#include "UObject/Class.h"
#include "CSDelegateFunction.generated.h"

UCLASS()
class UCSDelegateFunction : public UFunction, public ICSManagedTypeInterface
{
	GENERATED_BODY()
};
