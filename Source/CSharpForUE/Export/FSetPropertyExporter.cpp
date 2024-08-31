// Fill out your copyright notice in the Description page of Project Settings.

#include "FSetPropertyExporter.h"

void UFSetPropertyExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(GetScriptSetLayout)
	EXPORT_FUNCTION(GetElementProp)
}

FScriptSetLayout UFSetPropertyExporter::GetScriptSetLayout(const FSetProperty* SetProperty)
{
	return SetProperty->SetLayout;
}

void* UFSetPropertyExporter::GetElementProp(const FSetProperty* SetProperty)
{
	return SetProperty->ElementProp;
}
