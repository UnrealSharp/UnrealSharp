#pragma once

#include "CSGeneratedTypeBuilder.h"
#include "CSGeneratedInterfaceBuilder.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSGeneratedInterfaceBuilder : public UCSGeneratedTypeBuilder
{
	GENERATED_BODY()
 	DECLARE_BUILDER_TYPE(UCSInterface, FCSInterfaceMetaData)
public:
	// UCSGeneratedTypeBuilder interface implementation
	virtual void RebuildType() override;
	virtual UClass* GetFieldType() const override;
#if WITH_EDITOR
	virtual void UpdateType() override;
#endif
	// End of implementation

private:
	void RegisterFunctionsToLoader();
};
