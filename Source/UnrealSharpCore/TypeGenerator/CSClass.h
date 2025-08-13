#pragma once

#include "CoreMinimal.h"
#include "CSManagedTypeInterface.h"
#include "Engine/BlueprintGeneratedClass.h"
#include "CSClass.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSClass : public UBlueprintGeneratedClass, public ICSManagedTypeInterface
{
	GENERATED_BODY()
public:
#if WITH_EDITOR
	// UObject interface
	virtual void PostDuplicate(bool bDuplicateForPIE) override;
	// End of UObject interface
#endif
};