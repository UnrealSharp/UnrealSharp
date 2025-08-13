#pragma once

#include "CSGeneratedTypeBuilder.h"
#include "CSGeneratedEnumBuilder.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSGeneratedEnumBuilder : public UCSGeneratedTypeBuilder
{
	GENERATED_BODY()
 	DECLARE_BUILDER_TYPE(UCSEnum, FCSEnumMetaData)
public:
	
	// TCSGeneratedTypeBuilder interface implementation
	virtual void RebuildType() override;
	virtual UClass* GetFieldType() const override;
#if WITH_EDITOR
	virtual void UpdateType() override;
#endif
	// End of implementation

private:
	void PurgeEnum() const;
};
