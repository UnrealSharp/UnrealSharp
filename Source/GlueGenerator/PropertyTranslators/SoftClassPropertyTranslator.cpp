#include "SoftClassPropertyTranslator.h"

FString FSoftClassPropertyTranslator::GetManagedType(const FProperty* Property) const
{
	const FSoftObjectProperty* ClassProperty = CastFieldChecked<FSoftObjectProperty>(Property);
	return FString::Printf(TEXT("SoftClass<%s>"), *GetScriptNameMapper().GetQualifiedName(ClassProperty->PropertyClass));
}

bool FSoftClassPropertyTranslator::CanHandleProperty(const FProperty* Property) const
{
	return Property->GetClass() == FSoftClassProperty::StaticClass();
}

