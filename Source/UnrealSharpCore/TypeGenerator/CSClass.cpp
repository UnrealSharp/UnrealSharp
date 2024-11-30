#include "CSClass.h"

TSharedPtr<FCSharpClassInfo> UCSClass::GetClassInfo() const
{
	if (!ClassMetaData.IsValid())
	{
		return nullptr;
	}
	
	return ClassMetaData;
}
