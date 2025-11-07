#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "FMatrixExporter.generated.h"

UCLASS()
class UNREALSHARPCORE_API UFMatrixExporter : public UObject
{
	GENERATED_BODY()

public:

	UNREALSHARP_FUNCTION()
	static void FromRotator(FMatrix* Matrix, const FRotator Rotator);
	
};
