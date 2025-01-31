#include "CSClass.h"

TSharedRef<const FCSharpClassInfo> UCSClass::GetClassInfo() const
{
	return ClassMetaData.ToSharedRef();
}

void UCSClass::SetClassMetaData(const TSharedPtr<FCSharpClassInfo>& InClassMetaData)
{
	ClassMetaData = InClassMetaData;
}