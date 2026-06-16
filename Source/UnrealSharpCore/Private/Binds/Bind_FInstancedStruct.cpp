#include "CSBindsRegistry.h"
#include "StructUtils/InstancedStruct.h"

DECLARE_UNREALSHARP_BINDER(Bind_FInstancedStruct)
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
	
	BIND_UNREALSHARP_FUNCTION(GetNativeStruct)
	BIND_UNREALSHARP_FUNCTION(NativeInit)
	BIND_UNREALSHARP_FUNCTION(NativeCopy)
	BIND_UNREALSHARP_FUNCTION(NativeDestroy)
	BIND_UNREALSHARP_FUNCTION(InitializeAs)
	BIND_UNREALSHARP_FUNCTION(GetMemory)
}
