#pragma once

#include "CoreMinimal.h"
#include "UnrealSharpBinds.h"
#include "FVectorExporter.generated.h"

UCLASS(meta = (NotGeneratorValid))
class UNREALSHARPCORE_API UFVectorExporter : public UObject
{
	GENERATED_BODY()

public:

	UNREALSHARP_FUNCTION()
	static FVector FromRotator(const FRotator& Rotator);
	
};
