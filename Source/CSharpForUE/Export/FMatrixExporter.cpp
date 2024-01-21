// Fill out your copyright notice in the Description page of Project Settings.


#include "FMatrixExporter.h"

void UFMatrixExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(FromRotator)
}

void UFMatrixExporter::FromRotator(FMatrix& Matrix, const FRotator& Rotator)
{
	Matrix = Rotator.Quaternion().ToMatrix();
}
