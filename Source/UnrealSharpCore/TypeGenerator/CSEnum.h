#pragma once

#include "CoreMinimal.h"
#include "ManagedReferencesCollection.h"
#include "Engine/UserDefinedEnum.h"
#include "CSEnum.generated.h"

struct FCSEnumInfo;

UCLASS(MinimalAPI)
class UCSEnum : public UUserDefinedEnum
{
	GENERATED_BODY()

public:
	
	// UEnum interface
	virtual FString GenerateFullEnumName(const TCHAR* InEnumName) const override;
	// End of UEnum interface

	void SetEnumInfo(const TSharedPtr<FCSEnumInfo>& InEnumInfo);
	TSharedPtr<FCSEnumInfo> GetEnumInfo() const { return EnumInfo; }

#if WITH_EDITORONLY_DATA
	UPROPERTY(Transient)
	FCSManagedReferencesCollection ManagedReferences;
#endif

private:
	TSharedPtr<FCSEnumInfo> EnumInfo;
};
