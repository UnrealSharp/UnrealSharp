#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "FRotatorExporter.generated.h"

UCLASS()
class UNREALSHARPCORE_API UFRotatorExporter : public UObject
{
	GENERATED_BODY()

public:

	UNREALSHARP_FUNCTION()
	static void FromMatrix(FRotator* Rotator, const FMatrix& Matrix);
	
};
