#include "WeakObjectPropertyTranslator.h"
#include "PropertyTranslator.h"

FString FWeakObjectPropertyTranslator::GetManagedType(const FProperty *Property) const
{
	const FWeakObjectProperty* ObjectProperty = CastFieldChecked<FWeakObjectProperty>(Property);
	return FString::Printf(TEXT("WeakObject<%s>"), *GetScriptNameMapper().GetQualifiedName(ObjectProperty->PropertyClass));
}

bool FWeakObjectPropertyTranslator::CanHandleProperty(const FProperty* Property) const
{
	return Property->IsA(FWeakObjectProperty::StaticClass());
}

void FWeakObjectPropertyTranslator::AddReferences(const FProperty* Property, TSet<UField*>& References) const
{
	const FWeakObjectProperty* ObjectProperty = CastFieldChecked<FWeakObjectProperty>(Property);
	References.Add(ObjectProperty->PropertyClass);
}
