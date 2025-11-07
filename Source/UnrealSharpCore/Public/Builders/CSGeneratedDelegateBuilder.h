#pragma once

#include "CSGeneratedTypeBuilder.h"
#include "CSGeneratedDelegateBuilder.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSGeneratedDelegateBuilder : public UCSGeneratedTypeBuilder
{
	GENERATED_BODY()
public:
	UCSGeneratedDelegateBuilder();
	
	// TCSGeneratedTypeBuilder interface implementation
	virtual void RebuildType(UField* TypeToBuild, const TSharedPtr<FCSManagedTypeInfo>& ManagedTypeInfo) const override;
	// End of implementation
};
