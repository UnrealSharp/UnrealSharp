#include "UScriptStructExporter.h"

int UUScriptStructExporter::GetNativeStructSize(const UScriptStruct* ScriptStruct)
{
	if (const auto CppStructOps = ScriptStruct->GetCppStructOps())
	{
		return CppStructOps->GetSize();
	}
	
	return ScriptStruct->GetStructureSize();
}

