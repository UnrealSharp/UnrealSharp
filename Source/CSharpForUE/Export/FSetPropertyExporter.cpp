// Fill out your copyright notice in the Description page of Project Settings.

#include "FSetPropertyExporter.h"

void UFSetPropertyExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(GetScriptSetLayout)
}

void UFSetPropertyExporter::GetScriptSetLayout(FSetProperty* SetProperty, FScriptSetLayout* OutLayout)
{
	*OutLayout = SetProperty->SetLayout;
}
