#include "UScriptStructExporter.h"

int UUScriptStructExporter::GetNativeStructSize(const UScriptStruct* ScriptStruct)
{
	if (const auto CppStructOps = ScriptStruct->GetCppStructOps())
	{
		return CppStructOps->GetSize();
	}
	
	return ScriptStruct->GetStructureSize();
}

bool UUScriptStructExporter::NativeCopy(const UScriptStruct* ScriptStruct, void* Src, void* Dest)
{
	if (const auto CppStructOps = ScriptStruct->GetCppStructOps())
	{
		if (CppStructOps->HasCopy())
		{
			return CppStructOps->Copy(Dest, Src, 1);
		}
		else
		{
			FMemory::Memcpy(Dest, Src, CppStructOps->GetSize());
			return true;
		}
	}
	
	return false;
}

bool UUScriptStructExporter::NativeDestroy(const UScriptStruct* ScriptStruct, void* Struct) {
    if (const auto CppStructOps = ScriptStruct->GetCppStructOps(); CppStructOps != nullptr)
	{
		if (CppStructOps->HasDestructor())
		{
			CppStructOps->Destruct(Struct);
		}

        return true;
	}
	
	return false;
}

