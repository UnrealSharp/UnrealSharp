#pragma once

#include "PrimitiveTypePropertyTranslator.h"

class FBoolPropertyTranslator : public FSimpleTypePropertyTranslator
{
public:
	
	explicit FBoolPropertyTranslator(FPropertyTranslatorManager& InPropertyHandlers);

	//PropertyTranslator interface
	virtual FString GetPropertyName(const FProperty* Property) const override;
	//End of implementation
	
};
