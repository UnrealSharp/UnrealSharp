#pragma once

#include "CSGeneratedTypeBuilder.h"
#include "CSGeneratedDelegateBuilder.generated.h"

UCLASS()
class UNREALSHARPCORE_API UCSGeneratedDelegateBuilder : public UCSGeneratedTypeBuilder
{
	GENERATED_BODY()
	DECLARE_BUILDER_TYPE(UDelegateFunction, FCSDelegateMetaData)
public:
	// TCSGeneratedTypeBuilder interface implementation
	virtual void RebuildType() override;
	virtual UClass* GetFieldType() const override;
#if WITH_EDITOR
	virtual void UpdateType() override {}
#endif
	// End of implementation
};
