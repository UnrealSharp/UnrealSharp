#include "ObjectPropertyTranslator.h"

FObjectPropertyTranslator::FObjectPropertyTranslator(FCSPropertyTranslatorManager& InPropertyHandlers)
: FSimpleTypePropertyTranslator(InPropertyHandlers, FObjectProperty::StaticClass(), EPU_Any)
{

}

void FObjectPropertyTranslator::AddReferences(const FProperty* Property, TSet<UField*>& References) const
{
	const FObjectProperty& ObjectProperty = *CastFieldChecked<FObjectProperty>(Property);
	References.Add(ObjectProperty.PropertyClass);
}

FString FObjectPropertyTranslator::GetManagedType(const FProperty* Property) const
{
	const FObjectProperty* ObjectProperty = CastFieldChecked<FObjectProperty>(Property);
	return GetScriptNameMapper().GetQualifiedName(ObjectProperty->PropertyClass);
}

FString FObjectPropertyTranslator::GetMarshaller(const FProperty *Property) const
{
	const FObjectProperty* ObjectProperty = CastFieldChecked<FObjectProperty>(Property);
	const FString ObjectClass = GetScriptNameMapper().GetQualifiedName(ObjectProperty->PropertyClass);
	return FString::Printf(TEXT("ObjectMarshaller<%s>"), *ObjectClass);
}