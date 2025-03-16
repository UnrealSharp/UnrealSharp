#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "FVectorExporter.generated.h"

UCLASS()
class UNREALSHARPCORE_API UFVectorExporter : public UObject
{
	GENERATED_BODY()

public:

	UNREALSHARP_FUNCTION()
	static FVector FromRotator(const FRotator& Rotator);
	
};
