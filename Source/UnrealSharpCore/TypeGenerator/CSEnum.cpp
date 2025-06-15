#include "CSEnum.h"

FString UCSEnum::GenerateFullEnumName(const TCHAR* InEnumName) const
{
	return UEnum::GenerateFullEnumName(InEnumName);
}

void UCSEnum::SetEnumInfo(const TSharedPtr<FCSEnumInfo>& InEnumInfo)
{
	EnumInfo = InEnumInfo;
}
