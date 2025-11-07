#pragma once

#include "CoreMinimal.h"
#include "CSManagedTypeInterface.h"
#include "Engine/UserDefinedEnum.h"
#include "CSEnum.generated.h"

UCLASS(MinimalAPI)
class UCSEnum : public UUserDefinedEnum, public ICSManagedTypeInterface
{
	GENERATED_BODY()
public:
	// UEnum interface
	virtual FString GenerateFullEnumName(const TCHAR* InEnumName) const override { return UEnum::GenerateFullEnumName(InEnumName); }
	// End of UEnum interface
};
