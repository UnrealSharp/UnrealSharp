#pragma once

#include "CSGeneratedTypeBuilder.h"
#include "CSGeneratedStructBuilder.generated.h"

class UCSScriptStruct;

UCLASS()
class UNREALSHARPCORE_API UCSGeneratedStructBuilder : public UCSGeneratedTypeBuilder
{
	GENERATED_BODY()
	DECLARE_BUILDER_TYPE(UCSScriptStruct, FCSStructMetaData)
public:
	// TCSGeneratedTypeBuilder interface implementation
	virtual void RebuildType() override;
	virtual UClass* GetFieldType() const override;
#if WITH_EDITOR
	virtual void UpdateType() override;
#endif
	// End of implementation
private:
	void PurgeStruct();
};
