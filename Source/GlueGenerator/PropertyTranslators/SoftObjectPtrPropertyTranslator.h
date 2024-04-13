#pragma once
#include "BlittableStructPropertyTranslator.h"

class FSoftObjectPtrPropertyTranslator : public FBlittableStructPropertyTranslator
{
public:
	explicit FSoftObjectPtrPropertyTranslator(FCSPropertyTranslatorManager& InPropertyHandlers)
		: FBlittableStructPropertyTranslator(InPropertyHandlers)
	{
	}

	// FPropertyTranslator interface implementation
	virtual bool CanHandleProperty(const FProperty* Property) const override;
	virtual FString GetManagedType(const FProperty* Property) const override;
	virtual void AddReferences(const FProperty* Property, TSet<UField*>& References) const override;
	// End of implementation
};
