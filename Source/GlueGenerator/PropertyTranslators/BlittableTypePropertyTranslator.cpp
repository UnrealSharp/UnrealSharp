#include "BlittableTypePropertyTranslator.h"

FBlittableTypePropertyTranslator::FBlittableTypePropertyTranslator
(FCSPropertyTranslatorManager& InPropertyHandlers, FFieldClass* InPropertyClass, const FString& InCSharpType, EPropertyUsage InPropertyUsage)
: FSimpleTypePropertyTranslator(InPropertyHandlers, InPropertyClass, InCSharpType, InPropertyUsage)
{

}

FString FBlittableTypePropertyTranslator::GetMarshaller(const FProperty *Property) const
{
	return FString::Printf(TEXT("BlittableMarshaller<%s>"), *GetManagedType(Property));
}
