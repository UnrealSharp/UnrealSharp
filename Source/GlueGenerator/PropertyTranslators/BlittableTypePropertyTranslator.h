#pragma once
#include "PrimitiveTypePropertyTranslator.h"

class FBlittableTypePropertyTranslator : public FSimpleTypePropertyTranslator
{
public:
	
	FBlittableTypePropertyTranslator(FCSPropertyTranslatorManager& InPropertyHandlers, FFieldClass* InPropertyClass, const FString& InCSharpType, EPropertyUsage InPropertyUsage = EPropertyUsage::EPU_Any);

	//FPropertyTranslator interface implementation
	virtual bool IsBlittable() const override { return true; }
protected:
	virtual FString GetMarshaller(const FProperty *Property) const override;
	//End of implementation

};