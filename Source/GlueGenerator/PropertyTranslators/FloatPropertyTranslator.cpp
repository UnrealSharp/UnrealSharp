#include "FloatPropertyTranslator.h"

FFloatPropertyTranslator::FFloatPropertyTranslator(FPropertyTranslatorManager& InPropertyHandlers)
: FBlittableTypePropertyTranslator(InPropertyHandlers, FFloatProperty::StaticClass(), "float")
{

}

FString FFloatPropertyTranslator::ConvertCppDefaultParameterToCSharp(const FString& CppDefaultValue, UFunction* Function, FProperty* ParamProperty) const
{
	return CppDefaultValue + "f";
}
