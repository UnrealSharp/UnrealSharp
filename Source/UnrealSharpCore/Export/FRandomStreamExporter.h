#pragma once

#include "CoreMinimal.h"
#include "CSBindsManager.h"
#include "FRandomStreamExporter.generated.h"

UCLASS()
class UNREALSHARPCORE_API UFRandomStreamExporter : public UObject
{
	GENERATED_BODY()

public:

	UNREALSHARP_FUNCTION()
	static void GenerateNewSeed(FRandomStream* RandomStream);

	UNREALSHARP_FUNCTION()
	static float GetFraction(FRandomStream* RandomStream);

	UNREALSHARP_FUNCTION()
	static uint32 GetUnsignedInt(FRandomStream* RandomStream);

	UNREALSHARP_FUNCTION()
	static FVector GetUnitVector(FRandomStream* RandomStream);

	UNREALSHARP_FUNCTION()
	static int RandRange(FRandomStream* RandomStream, int32 Min, int32 Max);

	UNREALSHARP_FUNCTION()
	static FVector VRandCone(FRandomStream* RandomStream, FVector Dir, float ConeHalfAngleRad);

	UNREALSHARP_FUNCTION()
	static FVector VRandCone2(FRandomStream* RandomStream, FVector Dir, float HorizontalConeHalfAngleRad, float VerticalConeHalfAngleRad);
	
};
