#pragma once
#include "PrimitiveTypePropertyTranslator.h"

class FClassPropertyTranslator : public FSimpleTypePropertyTranslator
{
public:
	
	explicit FClassPropertyTranslator(FCSPropertyTranslatorManager& InPropertyHandlers);

	//FPropertyTranslator interface implementation
	virtual void AddReferences(const FProperty* Property, TSet<UField*>& References) const override;
	virtual FString GetManagedType(const FProperty* Property) const override;
protected:
	virtual FString GetMarshaller(const FProperty *Property) const override;
	//End of implementation
	
};