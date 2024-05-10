#pragma once

#include "CoreMinimal.h"
#include "Engine/UserDefinedEnum.h"
#include "CSEnum.generated.h"

UCLASS()
class CSHARPFORUE_API UCSEnum : public UUserDefinedEnum
{
	GENERATED_BODY()

public:
	
	// UEnum interface
	virtual FString GenerateFullEnumName(const TCHAR* InEnumName) const override;
	// End of UEnum interface
	
};
