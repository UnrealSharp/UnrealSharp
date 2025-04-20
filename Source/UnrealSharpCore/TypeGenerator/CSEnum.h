#pragma once

#include "CoreMinimal.h"
#include "ManagedReferencesCollection.h"
#include "Engine/UserDefinedEnum.h"
#include "CSEnum.generated.h"

struct FCSharpEnumInfo;

UCLASS(MinimalAPI)
class UCSEnum : public UUserDefinedEnum
{
	GENERATED_BODY()

public:
	
	// UEnum interface
	virtual FString GenerateFullEnumName(const TCHAR* InEnumName) const override;
	// End of UEnum interface

	void SetEnumInfo(const TSharedPtr<FCSharpEnumInfo>& InEnumInfo);
	TSharedPtr<FCSharpEnumInfo> GetEnumInfo() const { return EnumInfo; }

#if WITH_EDITORONLY_DATA
	UPROPERTY(Transient)
	FCSManagedReferencesCollection ManagedReferences;
#endif

private:
	TSharedPtr<FCSharpEnumInfo> EnumInfo;
};
