// Fill out your copyright notice in the Description page of Project Settings.


#include "FScriptMapHelperExporter.h"

void UFScriptMapHelperExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(AddPair)
	EXPORT_FUNCTION(FindOrAdd)
	EXPORT_FUNCTION(Num)
	EXPORT_FUNCTION(FindMapPairIndexFromHash)
	EXPORT_FUNCTION(RemoveIndex)
	EXPORT_FUNCTION(EmptyValues)
	EXPORT_FUNCTION(Remove)
	EXPORT_FUNCTION(IsValidIndex)
	EXPORT_FUNCTION(GetMaxIndex)
	EXPORT_FUNCTION(GetPairPtr)
}

void UFScriptMapHelperExporter::AddPair(FMapProperty* MapProperty, const void* Address, const void* Key, const void* Value)
{
	FScriptMapHelper Helper(MapProperty, Address);
	Helper.AddPair(Key, Value);
}

void* UFScriptMapHelperExporter::FindOrAdd(FMapProperty* MapProperty, const void* Address, const void* Key)
{
	FScriptMapHelper Helper(MapProperty, Address);
	return Helper.FindOrAdd(Key);
}

int UFScriptMapHelperExporter::Num(FMapProperty* MapProperty, const void* Address)
{
	FScriptMapHelper Helper(MapProperty, Address);
	return Helper.Num();
}

int UFScriptMapHelperExporter::FindMapPairIndexFromHash(FMapProperty* MapProperty, const void* Address, const void* Key)
{
	FScriptMapHelper Helper(MapProperty, Address);
#if ENGINE_MINOR_VERSION >= 4
	return Helper.FindMapPairIndexFromHash(Key);
#else
	return Helper.FindMapIndexWithKey(Key);
#endif
}

void UFScriptMapHelperExporter::RemoveIndex(FMapProperty* MapProperty, const void* Address, int Index)
{
	FScriptMapHelper Helper(MapProperty, Address);
	Helper.RemoveAt(Index);
}

void UFScriptMapHelperExporter::EmptyValues(FMapProperty* MapProperty, const void* Address)
{
	FScriptMapHelper Helper(MapProperty, Address);
	Helper.EmptyValues();
}

void UFScriptMapHelperExporter::Remove(FMapProperty* MapProperty, const void* Address, const void* Key)
{
	FScriptMapHelper Helper(MapProperty, Address);
	Helper.RemovePair(Key);
}

bool UFScriptMapHelperExporter::IsValidIndex(FMapProperty* MapProperty, const void* Address, int Index)
{
	FScriptMapHelper Helper(MapProperty, Address);
	return Helper.IsValidIndex(Index);
}

int UFScriptMapHelperExporter::GetMaxIndex(FMapProperty* MapProperty, const void* Address)
{
	FScriptMapHelper Helper(MapProperty, Address);
	return Helper.GetMaxIndex();
}

void* UFScriptMapHelperExporter::GetPairPtr(FMapProperty* MapProperty, const void* Address, int Index)
{
	FScriptMapHelper Helper(MapProperty, Address);
	return Helper.GetPairPtr(Index);
}
