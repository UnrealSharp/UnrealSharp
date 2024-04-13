#pragma once
#include "PropertyTranslator.h"

class FCSPropertyTranslatorManager;

class FNullPropertyTranslator : public FPropertyTranslator
{
public:
	
	FNullPropertyTranslator(FCSPropertyTranslatorManager& InPropertyHandlers);

	//FPropertyTranslator interface implementation
	virtual bool CanHandleProperty(const FProperty* Property) const override;
	virtual FString GetManagedType(const FProperty* Property) const override;
protected:
	virtual FString GetNullReturnCSharpValue(const FProperty* ReturnProperty) const override;
	//End of implementation
	
};
