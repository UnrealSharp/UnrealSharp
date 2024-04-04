#include "SoftObjectPtrPropertyTranslator.h"

bool FSoftObjectPtrPropertyTranslator::CanHandleProperty(const FProperty* Property) const
{
	return Property->GetClass() == FSoftObjectProperty::StaticClass();
}

FString FSoftObjectPtrPropertyTranslator::GetManagedType(const FProperty* Property) const
{
	const FSoftObjectProperty* ClassProperty = CastFieldChecked<FSoftObjectProperty>(Property);
	return FString::Printf(TEXT("SoftObject<%s>"), *GetScriptNameMapper().GetQualifiedName(ClassProperty->PropertyClass));
}

void FSoftObjectPtrPropertyTranslator::AddReferences(const FProperty* Property, TSet<UField*>& References) const
{
	const FSoftObjectProperty* ClassProperty = CastFieldChecked<FSoftObjectProperty>(Property);
	References.Add(ClassProperty->PropertyClass);
}
