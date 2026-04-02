#pragma once

#include "CoreMinimal.h"
#include "CSManagedTypeInterface.h"
#include "CSInterface.generated.h"

UCLASS(MinimalAPI)
class UCSInterface : public UClass, public ICSManagedTypeInterface
{
	GENERATED_BODY()
public:
	// UObject interface
	virtual bool IsFullNameStableForNetworking() const override { return true; }
	virtual bool IsNameStableForNetworking() const override { return true; }
	// End of UObject interface
};
