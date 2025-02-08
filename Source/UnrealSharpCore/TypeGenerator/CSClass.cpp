#include "CSClass.h"
#include "Register/TypeInfo/CSClassInfo.h"

TSharedRef<const FCSharpClassInfo> UCSClass::GetClassInfo() const
{
	return ClassInfo.ToSharedRef();
}

TWeakPtr<const FGCHandle> UCSClass::GetClassHandle() const
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
