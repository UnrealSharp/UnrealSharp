#include "BoolPropertyTranslator.h"

FBoolPropertyTranslator::FBoolPropertyTranslator(FCSPropertyTranslatorManager& InPropertyHandlers)
: FSimpleTypePropertyTranslator(InPropertyHandlers, FBoolProperty::StaticClass(), "bool", "BoolMarshaller", EPU_Any)
{

}

FString FBoolPropertyTranslator::GetPropertyName(const FProperty* Property) const
{
	return FSimpleTypePropertyTranslator::GetPropertyName(Property);
}
