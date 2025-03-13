#pragma once

#include "CoreMinimal.h"
#include "UnrealSharpBinds.h"
#include "FMatrixExporter.generated.h"

UCLASS()
class UNREALSHARPCORE_API UFMatrixExporter : public UObject
{
	GENERATED_BODY()

public:

	UNREALSHARP_FUNCTION()
	static void FromRotator(FMatrix& Matrix, const FRotator& Rotator);
	
};
