#pragma once

#include "CSGeneratedTypeBuilder.h"
#include "MetaData/CSInterfaceMetaData.h"
#include "TypeGenerator/CSInterface.h"

class UNREALSHARPCORE_API FCSGeneratedInterfaceBuilder : public TCSGeneratedTypeBuilder<FCSInterfaceMetaData, UCSInterface>
{
public:

	FCSGeneratedInterfaceBuilder(const TSharedPtr<FCSInterfaceMetaData>& InTypeMetaData, const TSharedPtr<FCSAssembly>& InOwningAssembly) : TCSGeneratedTypeBuilder(InTypeMetaData, InOwningAssembly) { }

	// TCSGeneratedTypeBuilder interface implementation
	virtual void RebuildType() override;
#if WITH_EDITOR
	virtual void UpdateType() override;
#endif
	// End of implementation

private:
	void RegisterFunctionsToLoader();
};
