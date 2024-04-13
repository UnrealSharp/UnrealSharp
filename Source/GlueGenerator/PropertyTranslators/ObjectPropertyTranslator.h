#pragma once
#include "PrimitiveTypePropertyTranslator.h"

class FObjectPropertyTranslator : public FSimpleTypePropertyTranslator
{
public:
	
	explicit FObjectPropertyTranslator(FCSPropertyTranslatorManager& InPropertyHandlers);

	//FPropertyTranslator interface implementation
	virtual void AddReferences(const FProperty* Property, TSet<UField*>& References) const override;
	virtual FString GetManagedType(const FProperty* Property) const override;
	virtual FString GetMarshaller(const FProperty *Property) const override;
	//End of implementation
	
};
