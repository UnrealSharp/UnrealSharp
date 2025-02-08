#pragma once

#include "CSGeneratedTypeBuilder.h"
#include "MetaData/CSInterfaceMetaData.h"

class UNREALSHARPCORE_API FCSGeneratedInterfaceBuilder : public TCSGeneratedTypeBuilder<FCSInterfaceMetaData, UClass>
{
public:

	FCSGeneratedInterfaceBuilder(const TSharedPtr<FCSInterfaceMetaData>& InTypeMetaData, const TSharedPtr<FCSAssembly>& InOwningAssembly) : TCSGeneratedTypeBuilder(InTypeMetaData, InOwningAssembly) { }

	// TCSGeneratedTypeBuilder interface implementation
	virtual void RebuildType() override;
	virtual void UpdateType() override;
	// End of implementation
};
