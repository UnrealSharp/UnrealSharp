#include "CSClass.h"
#include "Register/TypeInfo/CSClassInfo.h"

TSharedPtr<FCSClassInfo> UCSClass::GetClassInfo() const
{
	return ClassInfo;
}

TSharedPtr<const FGCHandle> UCSClass::GetClassHandle() const
{
	return ClassInfo->GetManagedTypeHandle();
}

TSharedPtr<FCSAssembly> UCSClass::GetOwningAssembly() const
{
	return ClassInfo->OwningAssembly;
}

void UCSClass::SetClassInfo(const TSharedPtr<FCSClassInfo>& InClassMetaData)
{
	ClassInfo = InClassMetaData;
}
