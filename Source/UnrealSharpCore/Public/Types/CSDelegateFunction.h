// Copyright Ember. All Rights Reserved.

#pragma once

#include "CoreMinimal.h"
#include "CSManagedTypeInterface.h"
#include "UObject/Class.h"
#include "CSDelegateFunction.generated.h"

UCLASS()
class UCSDelegateFunction : public UDelegateFunction, public ICSManagedTypeInterface
{
	GENERATED_BODY()
};
