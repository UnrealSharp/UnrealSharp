﻿#include "FRandomStreamExporter.h"

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

void UFRandomStreamExporter::GetUnitVector(FRandomStream* RandomStream, FVector& OutVector)
{
	OutVector = RandomStream->GetUnitVector();
}

int UFRandomStreamExporter::RandRange(FRandomStream* RandomStream, int32 Min, int32 Max)
{
	return RandomStream->RandRange(Min, Max);
}

void UFRandomStreamExporter::VRandCone(FRandomStream* RandomStream, FVector Dir, FVector& OutVector, float ConeHalfAngleRad)
{
	OutVector = RandomStream->VRandCone(Dir, ConeHalfAngleRad);
}

void UFRandomStreamExporter::VRandCone2(FRandomStream* RandomStream, FVector Dir, FVector& OutVector, float HorizontalConeHalfAngleRad, float VerticalConeHalfAngleRad)
{
	OutVector = RandomStream->VRandCone(Dir, HorizontalConeHalfAngleRad, VerticalConeHalfAngleRad);
}

