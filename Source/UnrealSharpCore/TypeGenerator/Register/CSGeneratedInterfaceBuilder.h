#pragma once

#include "CSGeneratedTypeBuilder.h"
#include "MetaData/CSInterfaceMetaData.h"

class UNREALSHARPCORE_API FCSGeneratedInterfaceBuilder : public TCSGeneratedTypeBuilder<FCSInterfaceMetaData, UClass>
{
public:

	FCSGeneratedInterfaceBuilder(const TSharedPtr<FCSInterfaceMetaData>& InTypeMetaData) : TCSGeneratedTypeBuilder(InTypeMetaData) { }

	// TCSGeneratedTypeBuilder interface implementation
	virtual void StartBuildingType() override;
#if WITH_EDITOR
	virtual void OnFieldReplaced(UClass* OldField, UClass* NewField) override;
#endif
	// End of implementation
};
