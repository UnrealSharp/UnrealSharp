#include "CSScriptStruct.h"

void UCSScriptStruct::PrepareCppStructOps()
{
	if (!CppStructOps)
	{
		CppStructOps = new FUSCppStructOps(GetStructureSize(), GetMinAlignment(), this);
	}
	
	UScriptStruct::PrepareCppStructOps();
}

