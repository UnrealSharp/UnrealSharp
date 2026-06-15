#include "CSBindsManager.h"
#include "StructUtils/InstancedStruct.h"

DECLARE_UNREALSHARP_EXPORTER(FInstancedStructExporter)
{
	const UScriptStruct* GetNativeStruct(const FInstancedStruct& Struct)
	{
		check(&Struct != nullptr);
		return Struct.GetScriptStruct();
	}

	void NativeInit(FInstancedStruct& Struct)
	{
		std::construct_at(&Struct);
	}

	void NativeCopy(FInstancedStruct& Dest, const FInstancedStruct& Src)
	{
		std::construct_at(&Dest, Src);
	}

	void NativeDestroy(FInstancedStruct& Struct)
	{
		std::destroy_at(&Struct);
	}

	void InitializeAs(FInstancedStruct& Struct, const UScriptStruct* ScriptStruct, const uint8* StructData)
	{
		check(ScriptStruct != nullptr);
		Struct.InitializeAs(ScriptStruct, StructData);
	}

	const uint8* GetMemory(const FInstancedStruct& Struct)
	{
		return Struct.GetMemory();
	}
	
	EXPORT_UNREALSHARP_FUNCTION(GetNativeStruct)
	EXPORT_UNREALSHARP_FUNCTION(NativeInit)
	EXPORT_UNREALSHARP_FUNCTION(NativeCopy)
	EXPORT_UNREALSHARP_FUNCTION(NativeDestroy)
	EXPORT_UNREALSHARP_FUNCTION(InitializeAs)
	EXPORT_UNREALSHARP_FUNCTION(GetMemory)
}
