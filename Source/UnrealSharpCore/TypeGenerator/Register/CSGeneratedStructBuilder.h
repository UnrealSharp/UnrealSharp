#pragma once

#include "CSGeneratedTypeBuilder.h"
#include "CSGeneratedStructBuilder.generated.h"

class UCSScriptStruct;

UCLASS()
class UNREALSHARPCORE_API UCSGeneratedStructBuilder : public UCSGeneratedTypeBuilder
{
	GENERATED_BODY()
public:
	UCSGeneratedStructBuilder();
	
	// TCSGeneratedTypeBuilder interface implementation
	virtual void RebuildType(UField* TypeToBuild, const TSharedPtr<FCSManagedTypeInfo>& ManagedTypeInfo) const override;
	// End of implementation
private:
	static void PurgeStruct(UCSScriptStruct* Field);
};
