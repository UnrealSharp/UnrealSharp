#pragma once
#include "BlittableTypePropertyTranslator.h"

class FFloatPropertyTranslator : public FBlittableTypePropertyTranslator
{
public:
	
	explicit FFloatPropertyTranslator(FPropertyTranslatorManager& InPropertyHandlers);

protected:

	//FPropertyTranslator interface implementation
	virtual FString ConvertCppDefaultParameterToCSharp(const FString& CppDefaultValue, UFunction* Function, FProperty* ParamProperty) const override;
	//End of implementation
	
};
