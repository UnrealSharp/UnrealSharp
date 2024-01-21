#pragma once

#include "CSGeneratedTypeBuilder.h"
#include "CSTypeRegistry.h"

class CSHARPFORUE_API FCSGeneratedEnumBuilder : public TCSGeneratedTypeBuilder<FEnumMetaData, UEnum>
{
	
public:
	
	FCSGeneratedEnumBuilder(const TSharedPtr<FEnumMetaData>& InTypeMetaData) : TCSGeneratedTypeBuilder(InTypeMetaData) { }

	// TCSGeneratedTypeBuilder interface implementation
	virtual void StartBuildingType() override;
	virtual void NewField(UEnum* OldField, UEnum* NewField) override;
	// End of implementation
};
