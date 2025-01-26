#pragma once

#include "CSGeneratedTypeBuilder.h"
#include "MetaData/CSEnumMetaData.h"
#include "TypeGenerator/CSEnum.h"

class UNREALSHARPCORE_API FCSGeneratedEnumBuilder : public TCSGeneratedTypeBuilder<FCSEnumMetaData, UCSEnum>
{
	
public:
	
	FCSGeneratedEnumBuilder(const TSharedPtr<FCSEnumMetaData>& InTypeMetaData) : TCSGeneratedTypeBuilder(InTypeMetaData) { }

	// TCSGeneratedTypeBuilder interface implementation
	virtual void StartBuildingType() override;
	// End of implementation
};
