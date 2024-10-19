// Fill out your copyright notice in the Description page of Project Settings.


#include "UScriptStructExporter.h"

void UUScriptStructExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(GetNativeStructSize)
}

int UUScriptStructExporter::GetNativeStructSize(const UScriptStruct* ScriptStruct)
{
	if (ScriptStruct->GetCppStructOps())
	{
		return ScriptStruct->GetCppStructOps()->GetSize();
	}
	
	return ScriptStruct->GetStructureSize();
}
