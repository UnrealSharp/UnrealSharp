#pragma once

#include "CoreMinimal.h"
#include "CSManagedTypeInterface.h"
#include "UObject/Class.h"
#include "CSDelegateFunction.generated.h"

UCLASS()
class UCSDelegateFunction : public UFunction, public ICSManagedTypeInterface
{
	GENERATED_BODY()

public:
	// Delegate signatures are package-owned; UFunction is Within=Class in UE 5.6.
	DECLARE_WITHIN(UObject)
};
