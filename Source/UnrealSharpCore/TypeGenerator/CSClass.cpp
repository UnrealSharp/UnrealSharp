#include "CSClass.h"

TSharedRef<FCSharpClassInfo> UCSClass::GetClassInfo() const
{
	return ClassMetaData.ToSharedRef();
}
