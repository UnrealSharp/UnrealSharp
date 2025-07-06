#include "FRandomStreamExporter.h"

void UFRandomStreamExporter::GenerateNewSeed(FRandomStream* RandomStream)
{
	RandomStream->GenerateNewSeed();
}

float UFRandomStreamExporter::GetFraction(FRandomStream* RandomStream)
{
	return RandomStream->GetFraction();
}

uint32 UFRandomStreamExporter::GetUnsignedInt(FRandomStream* RandomStream)
{
	return RandomStream->GetUnsignedInt();
}

FVector UFRandomStreamExporter::GetUnitVector(FRandomStream* RandomStream)
{
	return RandomStream->GetUnitVector();
}

int UFRandomStreamExporter::RandRange(FRandomStream* RandomStream, int32 Min, int32 Max)
{
	return RandomStream->RandRange(Min, Max);
}

FVector UFRandomStreamExporter::VRandCone(FRandomStream* RandomStream, FVector Dir, float ConeHalfAngleRad)
{
	return RandomStream->VRandCone(Dir, ConeHalfAngleRad);
}

FVector UFRandomStreamExporter::VRandCone2(FRandomStream* RandomStream, FVector Dir, float HorizontalConeHalfAngleRad, float VerticalConeHalfAngleRad)
{
	return RandomStream->VRandCone(Dir, HorizontalConeHalfAngleRad, VerticalConeHalfAngleRad);
}

