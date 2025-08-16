#pragma once

#include "CSGeneratedTypeBuilder.h"
#include "CSGeneratedEnumBuilder.generated.h"

class UCSEnum;

UCLASS()
class UNREALSHARPCORE_API UCSGeneratedEnumBuilder : public UCSGeneratedTypeBuilder
{
	GENERATED_BODY()
public:
	
	// TCSGeneratedTypeBuilder interface implementation
	virtual void RebuildType(UField* TypeToBuild, const TSharedPtr<FCSManagedTypeInfo>& ManagedTypeInfo) const override;
	virtual UClass* GetFieldType() const override;
	// End of implementation

private:
	static void PurgeEnum(UCSEnum* Enum);
};
