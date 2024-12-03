#include "CSClass.h"

TSharedPtr<const FCSharpClassInfo> UCSClass::GetClassInfo() const
{
	if (!ClassMetaData.IsValid())
	{
		return nullptr;
	}
	
	return ClassMetaData;
}

void UCSClass::SetClassMetaData(const TSharedPtr<FCSharpClassInfo>& InClassMetaData)
{
	if (ClassMetaData.IsValid())
	{
		UE_LOG(LogUnrealSharp, Warning, TEXT("ClassMetaData already set for %s"), *GetName());
		return;
	}
		
	ClassMetaData = InClassMetaData;
}
