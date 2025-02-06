#include "CSClass.h"
#include "Register/TypeInfo/CSClassInfo.h"

TSharedRef<const FCSharpClassInfo> UCSClass::GetClassInfo() const
{
	return ClassInfo.ToSharedRef();
}

TSharedRef<const FGCHandle> UCSClass::GetClassHandle() const
{
	return ClassInfo->TypeHandle.ToSharedRef();
}

TSharedPtr<FCSAssembly> UCSClass::GetOwningAssembly() const
{
	return nullptr;
}

void UCSClass::SetClassInfo(const TSharedPtr<FCSharpClassInfo>& InClassMetaData)
{
	ClassInfo = InClassMetaData;
}
