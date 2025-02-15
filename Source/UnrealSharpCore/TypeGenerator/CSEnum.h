#pragma once

#include "CoreMinimal.h"
#include "Engine/UserDefinedEnum.h"
#include "CSEnum.generated.h"

struct FCSharpEnumInfo;

UCLASS()
class UNREALSHARPCORE_API UCSEnum : public UUserDefinedEnum
{
	GENERATED_BODY()

public:
	
	// UEnum interface
	virtual FString GenerateFullEnumName(const TCHAR* InEnumName) const override;
	// End of UEnum interface

	void SetEnumInfo(const TSharedPtr<FCSharpEnumInfo>& InEnumInfo);
	TSharedPtr<FCSharpEnumInfo> GetEnumInfo() const { return EnumInfo; }

private:
	TSharedPtr<FCSharpEnumInfo> EnumInfo;
};
