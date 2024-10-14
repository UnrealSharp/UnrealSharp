// Fill out your copyright notice in the Description page of Project Settings.


#include "FVectorExporter.h"

void UFVectorExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(FromRotator)
}

FVector UFVectorExporter::FromRotator(const FRotator& Rotator)
{
	return Rotator.Vector();
}
