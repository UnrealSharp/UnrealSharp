#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "FVectorExporter.generated.h"

UCLASS()
class UFVectorExporter : public UObject
{
	GENERATED_BODY()

public:

	UNREALSHARP_FUNCTION()
	static FVector FromRotator(FRotator Rotator);
	
};
