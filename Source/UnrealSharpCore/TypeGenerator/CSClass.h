#pragma once

#include "CoreMinimal.h"
#include "Engine/BlueprintGeneratedClass.h"
#include "Utils/CSMacros.h"
#include "CSClass.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSClass : public UBlueprintGeneratedClass
{
	GENERATED_BODY()
	DECLARE_CSHARP_TYPE_FUNCTIONS(FCSClassInfo)
public:
#if WITH_EDITOR
	// UObject interface
	virtual void PostDuplicate(bool bDuplicateForPIE) override;
	// End of UObject interface
#endif
};