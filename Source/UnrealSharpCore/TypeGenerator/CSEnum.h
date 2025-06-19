#pragma once

#include "CoreMinimal.h"
#include "ManagedReferencesCollection.h"
#include "Engine/UserDefinedEnum.h"
#include "Utils/CSMacros.h"
#include "CSEnum.generated.h"

UCLASS(MinimalAPI)
class UCSEnum : public UUserDefinedEnum
{
	GENERATED_BODY()
	DECLARE_CSHARP_TYPE_FUNCTIONS(FCSEnumInfo)
public:
	
	// UEnum interface
	virtual FString GenerateFullEnumName(const TCHAR* InEnumName) const override;
	// End of UEnum interface

#if WITH_EDITORONLY_DATA
	UPROPERTY(Transient)
	FCSManagedReferencesCollection ManagedReferences;
#endif
};
