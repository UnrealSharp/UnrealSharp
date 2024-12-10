#include "CSClass.h"
#include "UnrealSharpCore.h"

TSharedRef<const FCSharpClassInfo> UCSClass::GetClassInfo() const
{
	return ClassMetaData.ToSharedRef();
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
