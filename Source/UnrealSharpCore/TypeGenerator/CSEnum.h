#pragma once

#include "CoreMinimal.h"
#include "CSManagedTypeInterface.h"
#include "ManagedReferencesCollection.h"
#include "Engine/UserDefinedEnum.h"
#include "CSEnum.generated.h"

UCLASS(MinimalAPI)
class UCSEnum : public UUserDefinedEnum, public ICSManagedTypeInterface
{
	GENERATED_BODY()
public:
	
	// UEnum interface
	virtual FString GenerateFullEnumName(const TCHAR* InEnumName) const override;
	// End of UEnum interface

#if WITH_EDITORONLY_DATA
	UPROPERTY(Transient)
	FCSManagedReferencesCollection ManagedReferences;
#endif
};
