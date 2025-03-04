#include "CSScriptStruct.h"

void UCSScriptStruct::RecreateDefaults()
{
	DefaultStructInstance.Recreate(this);
}

void UCSScriptStruct::SetStructInfo(const TSharedPtr<FCSharpStructInfo>& InStructInfo)
{
	StructInfo = InStructInfo;
}
