#pragma once

#include "CSGeneratedTypeBuilder.h"
#include "MetaData/CSInterfaceMetaData.h"

class UNREALSHARPCORE_API FCSGeneratedInterfaceBuilder : public TCSGeneratedTypeBuilder<FCSInterfaceMetaData, UClass>
{
public:

	FCSGeneratedInterfaceBuilder(const TSharedPtr<FCSInterfaceMetaData>& InTypeMetaData) : TCSGeneratedTypeBuilder(InTypeMetaData) { }

	// TCSGeneratedTypeBuilder interface implementation
	virtual void StartBuildingType() override;
	// End of implementation
};
