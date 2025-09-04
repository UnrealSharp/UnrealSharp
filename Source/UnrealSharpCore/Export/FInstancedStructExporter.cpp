// Fill out your copyright notice in the Description page of Project Settings.


#include "FInstancedStructExporter.h"

#include "StructUtils/InstancedStruct.h"

const UScriptStruct* UFInstancedStructExporter::GetNativeStruct(const FInstancedStruct& Struct)
{
	check(&Struct != nullptr);
	return Struct.GetScriptStruct();
}

void UFInstancedStructExporter::NativeInit(FInstancedStruct& Struct)
{
	std::construct_at(&Struct);
}

void UFInstancedStructExporter::NativeCopy(FInstancedStruct& Dest, const FInstancedStruct& Src)
{
	std::construct_at(&Dest, Src);
}

void UFInstancedStructExporter::NativeDestroy(FInstancedStruct& Struct)
{
	std::destroy_at(&Struct);
}

void UFInstancedStructExporter::InitializeAs(FInstancedStruct& Struct, const UScriptStruct* ScriptStruct, const uint8* StructData)
{
	check(ScriptStruct != nullptr);
	Struct.InitializeAs(ScriptStruct, StructData);
}

const uint8* UFInstancedStructExporter::GetMemory(const FInstancedStruct& Struct)
{
	return Struct.GetMemory();
}
