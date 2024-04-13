#pragma once
#include "PrimitiveTypePropertyTranslator.h"
#include "PropertyTranslator.h"

class FCSInterfacePropertyTranslator : public FSimpleTypePropertyTranslator
{
public:
	
	FCSInterfacePropertyTranslator(FCSPropertyTranslatorManager& InPropertyHandlers)
	: FSimpleTypePropertyTranslator(InPropertyHandlers, FInterfaceProperty::StaticClass(), EPU_Any)
	{
	}

	//FPropertyTranslator interface implementation
	virtual bool CanHandleProperty(const FProperty* Property) const override;
	virtual FString GetManagedType(const FProperty* Property) const override;
	virtual FString GetMarshaller(const FProperty* Property) const override;
protected:
	virtual void AddReferences(const FProperty* Property, TSet<UField*>& References) const override;
	//End of implementation
	
};
