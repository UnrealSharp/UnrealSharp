#pragma once

#include "CSGeneratedTypeBuilder.h"
#include "MetaData/CSEnumMetaData.h"
#include "TypeGenerator/CSEnum.h"

class UNREALSHARPCORE_API FCSGeneratedEnumBuilder : public TCSGeneratedTypeBuilder<FCSEnumMetaData, UCSEnum>
{
	
public:
	
	FCSGeneratedEnumBuilder(const TSharedPtr<FCSEnumMetaData>& InTypeMetaData, const TSharedPtr<FCSAssembly>& InOwningAssembly) : TCSGeneratedTypeBuilder(InTypeMetaData, InOwningAssembly) { }

	// TCSGeneratedTypeBuilder interface implementation
	virtual void RebuildType() override;
#if WITH_EDITOR
	virtual void UpdateType() override;
#endif
	// End of implementation

private:
	void PurgeEnum() const;
};
