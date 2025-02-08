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
	virtual void UpdateType() override;
	// End of implementation
};
