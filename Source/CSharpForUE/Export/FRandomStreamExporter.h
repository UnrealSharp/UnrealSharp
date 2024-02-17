#pragma once

#include "CoreMinimal.h"
#include "FunctionsExporter.h"
#include "FRandomStreamExporter.generated.h"

UCLASS(meta = (NotGeneratorValid))
class CSHARPFORUE_API UFRandomStreamExporter : public UFunctionsExporter
{
	GENERATED_BODY()

public:

	// UFunctionsExporter interface implementation
	virtual void ExportFunctions(FRegisterExportedFunction RegisterExportedFunction) override;
	// End

private:

	static void GenerateNewSeed(FRandomStream* RandomStream);
	static float GetFraction(FRandomStream* RandomStream);
	static uint32 GetUnsignedInt(FRandomStream* RandomStream);
	static void GetUnitVector(FRandomStream* RandomStream, FVector& OutVector);
	static int RandRange(FRandomStream* RandomStream, int32 Min, int32 Max);
	static void VRandCone(FRandomStream* RandomStream, FVector Dir, FVector& OutVector, float ConeHalfAngleRad);
	static void VRandCone2(FRandomStream* RandomStream, FVector Dir, FVector& OutVector, float HorizontalConeHalfAngleRad, float VerticalConeHalfAngleRad);
	
};
