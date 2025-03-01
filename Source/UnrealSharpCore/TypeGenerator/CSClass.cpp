#include "CSClass.h"
#include "Register/TypeInfo/CSClassInfo.h"

TSharedPtr<FCSharpClassInfo> UCSClass::GetClassInfo() const
{
	return ClassInfo;
}

TSharedPtr<const FGCHandle> UCSClass::GetClassHandle() const
{
	return ClassInfo->GetTypeHandle();
}

TSharedPtr<FCSAssembly> UCSClass::GetOwningAssembly() const
{
	return ClassInfo->OwningAssembly;
}

void UCSClass::SetClassInfo(const TSharedPtr<FCSharpClassInfo>& InClassMetaData)
{
	ClassInfo = InClassMetaData;
}
