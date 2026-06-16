#include "CSBindsRegistry.h"

DECLARE_UNREALSHARP_BINDER(Bind_FRandomStream)
{
	void GenerateNewSeed(FRandomStream* RandomStream)
	{
		RandomStream->GenerateNewSeed();
	}

	float GetFraction(FRandomStream* RandomStream)
	{
		return RandomStream->GetFraction();
	}

	uint32 GetUnsignedInt(FRandomStream* RandomStream)
	{
		return RandomStream->GetUnsignedInt();
	}

	FVector GetUnitVector(FRandomStream* RandomStream)
	{
		return RandomStream->GetUnitVector();
	}

	int RandRange(FRandomStream* RandomStream, int32 Min, int32 Max)
	{
		return RandomStream->RandRange(Min, Max);
	}

	FVector VRandCone(FRandomStream* RandomStream, FVector Dir, float ConeHalfAngleRad)
	{
		return RandomStream->VRandCone(Dir, ConeHalfAngleRad);
	}

	FVector VRandCone2(FRandomStream* RandomStream, FVector Dir, float HorizontalConeHalfAngleRad, float VerticalConeHalfAngleRad)
	{
		return RandomStream->VRandCone(Dir, HorizontalConeHalfAngleRad, VerticalConeHalfAngleRad);
	}

	BIND_UNREALSHARP_FUNCTION(GenerateNewSeed)
	BIND_UNREALSHARP_FUNCTION(GetFraction)
	BIND_UNREALSHARP_FUNCTION(GetUnsignedInt)
	BIND_UNREALSHARP_FUNCTION(GetUnitVector)
	BIND_UNREALSHARP_FUNCTION(RandRange)
	BIND_UNREALSHARP_FUNCTION(VRandCone)
	BIND_UNREALSHARP_FUNCTION(VRandCone2)
}
