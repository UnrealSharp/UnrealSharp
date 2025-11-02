#pragma once

#include "CSGeneratedTypeBuilder.h"
#include "CSGeneratedInterfaceBuilder.generated.h"

class UCSInterface;

UCLASS()
class UNREALSHARPCORE_API UCSGeneratedInterfaceBuilder : public UCSGeneratedTypeBuilder
{
	GENERATED_BODY()
public:
	UCSGeneratedInterfaceBuilder();
	
	// UCSGeneratedTypeBuilder interface implementation
	virtual void RebuildType(UField* TypeToBuild, const TSharedPtr<FCSManagedTypeInfo>& ManagedTypeInfo) const override;
	// End of implementation
};
