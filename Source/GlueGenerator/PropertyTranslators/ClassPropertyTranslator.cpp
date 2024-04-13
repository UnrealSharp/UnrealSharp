#include "ClassPropertyTranslator.h"

FClassPropertyTranslator::FClassPropertyTranslator(FCSPropertyTranslatorManager& InPropertyHandlers)
: FSimpleTypePropertyTranslator(InPropertyHandlers, FClassProperty::StaticClass(), EPU_Any)
{

}

void FClassPropertyTranslator::AddReferences(const FProperty* Property, TSet<UField*>& References) const
{
	const FClassProperty* ClassProperty = CastFieldChecked<FClassProperty>(Property);
	References.Add(ClassProperty->MetaClass);
}

FString FClassPropertyTranslator::GetManagedType(const FProperty* Property) const
{
	const FClassProperty* ClassProperty = CastFieldChecked<FClassProperty>(Property);
	return FString::Printf(TEXT("SubclassOf<%s>"), *GetScriptNameMapper().GetQualifiedName(ClassProperty->MetaClass));
}

FString FClassPropertyTranslator::GetMarshaller(const FProperty* Property) const
{
	const FClassProperty* ClassProperty = CastFieldChecked<FClassProperty>(Property);
	return FString::Printf(TEXT("SubclassOfMarshaller<%s>"), *GetScriptNameMapper().GetQualifiedName(ClassProperty->MetaClass));
}

