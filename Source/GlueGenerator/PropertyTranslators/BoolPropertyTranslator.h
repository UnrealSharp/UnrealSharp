#pragma once

#include "CSSupportedPropertyTranslators.h"
#include "PrimitiveTypePropertyTranslator.h"

class FBoolPropertyTranslator : public FSimpleTypePropertyTranslator
{
public:
	
	explicit FBoolPropertyTranslator(FCSSupportedPropertyTranslators& InPropertyHandlers);

	//PropertyTranslator interface
	virtual FString GetPropertyName(const FProperty* Property) const override;
	//End of implementation
	
};
