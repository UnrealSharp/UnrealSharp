#pragma once

#include "CSGeneratedTypeBuilder.h"
#include "MetaData/CSDelegateMetaData.h"

class UNREALSHARPCORE_API FCSGeneratedDelegateBuilder : public TCSGeneratedTypeBuilder<FCSDelegateMetaData, UDelegateFunction>
{
public:
	FCSGeneratedDelegateBuilder(const TSharedPtr<FCSDelegateMetaData>& InTypeMetaData, const TSharedPtr<FCSAssembly>& InOwningAssembly)
	: TCSGeneratedTypeBuilder(InTypeMetaData, InOwningAssembly) { }

	// TCSGeneratedTypeBuilder interface implementation
	virtual void RebuildType() override;
#if WITH_EDITOR
	virtual void UpdateType() override {}
#endif
	// End of implementation
};
