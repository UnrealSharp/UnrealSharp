#pragma once
#include "BlittableStructPropertyTranslator.h"
#include "SoftObjectPtrPropertyTranslator.h"

class FSoftClassPropertyTranslator : public FSoftObjectPtrPropertyTranslator
{
public:
	explicit FSoftClassPropertyTranslator(FCSPropertyTranslatorManager& InPropertyHandlers)
		: FSoftObjectPtrPropertyTranslator(InPropertyHandlers)
	{
	}

	// FPropertyTranslator interface implementation
	virtual FString GetManagedType(const FProperty* Property) const override;
	virtual bool CanHandleProperty(const FProperty* Property) const override;
	// End of implementation
	
};
