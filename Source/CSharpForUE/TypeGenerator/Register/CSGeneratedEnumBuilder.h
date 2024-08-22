#pragma once

#include "CSGeneratedTypeBuilder.h"
#include "MetaData/CSEnumMetaData.h"
#include "TypeGenerator/CSEnum.h"

class CSHARPFORUE_API FCSGeneratedEnumBuilder : public TCSGeneratedTypeBuilder<FCSEnumMetaData, UCSEnum>
{
	
public:
	
	FCSGeneratedEnumBuilder(const TSharedPtr<FCSEnumMetaData>& InTypeMetaData) : TCSGeneratedTypeBuilder(InTypeMetaData) { }

	// TCSGeneratedTypeBuilder interface implementation
	virtual void StartBuildingType() override;
	virtual bool ReplaceTypeOnReload() const override { return false; }
	// End of implementation
};
