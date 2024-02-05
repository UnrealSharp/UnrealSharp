#pragma once
#include "PropertyTranslator.h"

class FMulticastDelegatePropertyTranslator : public FPropertyTranslator
{
public:
	FMulticastDelegatePropertyTranslator(FCSSupportedPropertyTranslators& InPropertyHandlers) : FPropertyTranslator(InPropertyHandlers, EPU_Property)
	{
	}

	// FPropertyTranslator interface implementation
	virtual bool CanHandleProperty(const FProperty* Property) const override;
	virtual FString GetManagedType(const FProperty* Property) const override;
	// End of implementation

protected:
	virtual FString GetNullReturnCSharpValue(const FProperty* ReturnProperty) const override;
};
