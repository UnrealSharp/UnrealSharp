#include "InterfacePropertyTranslator.h"
#include "GlueGenerator/CSGenerator.h"

bool FCSInterfacePropertyTranslator::CanHandleProperty(const FProperty* Property) const
{
	return Property->IsA<FInterfaceProperty>();
}

FString FCSInterfacePropertyTranslator::GetManagedType(const FProperty* Property) const
{
	const FInterfaceProperty* InterfaceProperty = CastFieldChecked<FInterfaceProperty>(Property);
	const FString& Namespace = FCSGenerator::Get().GetNamespace(InterfaceProperty->InterfaceClass);
	return FString::Printf(TEXT("%s.I%s"), *Namespace, *InterfaceProperty->InterfaceClass->GetName());
}

FString FCSInterfacePropertyTranslator::GetMarshaller(const FProperty* Property) const
{
	const FInterfaceProperty* InterfaceProperty = CastFieldChecked<FInterfaceProperty>(Property);
	const FString& Namespace = FCSGenerator::Get().GetNamespace(InterfaceProperty->InterfaceClass);
	return FString::Printf(TEXT("%s.I%sMarshaller"), *Namespace, *InterfaceProperty->InterfaceClass->GetName());
}

void FCSInterfacePropertyTranslator::AddReferences(const FProperty* Property, TSet<UField*>& References) const
{
	FPropertyTranslator::AddReferences(Property, References);
	const FInterfaceProperty* InterfaceProperty = CastFieldChecked<FInterfaceProperty>(Property);
	References.Add(InterfaceProperty->InterfaceClass);
}
