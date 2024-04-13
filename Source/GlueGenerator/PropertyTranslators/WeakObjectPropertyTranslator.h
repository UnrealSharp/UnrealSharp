#pragma once

#include "BlittableStructPropertyTranslator.h"

class FWeakObjectPropertyTranslator : public FBlittableStructPropertyTranslator
{
public:
	explicit FWeakObjectPropertyTranslator(FCSPropertyTranslatorManager& InPropertyHandlers)
		: FBlittableStructPropertyTranslator(InPropertyHandlers)
	{
	}

	//FPropertyTranslator interface implementation
	virtual FString GetManagedType(const FProperty* Property) const override;
	virtual bool CanHandleProperty(const FProperty* Property) const override;
	virtual void AddReferences(const FProperty* Property, TSet<UField*>& References) const override;
	virtual bool CanExportDefaultParameter() const override { return false; }
	//End of implementation
	
};
