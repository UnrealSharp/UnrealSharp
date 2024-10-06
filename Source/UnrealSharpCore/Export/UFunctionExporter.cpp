// Fill out your copyright notice in the Description page of Project Settings.

#include "UFunctionExporter.h"

void UUFunctionExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(GetNativeFunctionParamsSize)
}

uint16 UUFunctionExporter::GetNativeFunctionParamsSize(const UFunction* NativeFunction)
{
	check(NativeFunction);
	return NativeFunction->ParmsSize;
}