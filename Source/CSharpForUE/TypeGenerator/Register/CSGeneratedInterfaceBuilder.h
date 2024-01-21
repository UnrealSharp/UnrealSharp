#pragma once

#include "CSGeneratedTypeBuilder.h"
#include "CSTypeRegistry.h"

struct FInterfaceMetaData;

class CSHARPFORUE_API FCSGeneratedInterfaceBuilder : public TCSGeneratedTypeBuilder<FInterfaceMetaData, UClass>
{
public:

	FCSGeneratedInterfaceBuilder(const TSharedPtr<FInterfaceMetaData>& InTypeMetaData) : TCSGeneratedTypeBuilder(InTypeMetaData) { }

	// TCSGeneratedTypeBuilder interface implementation
	virtual void StartBuildingType() override;
	virtual void NewField(UClass* OldField, UClass* NewField) override;
	// End of implementation
};
