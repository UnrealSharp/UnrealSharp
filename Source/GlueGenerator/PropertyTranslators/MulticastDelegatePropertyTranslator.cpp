#include "MulticastDelegatePropertyTranslator.h"

bool FMulticastDelegatePropertyTranslator::CanHandleProperty(const FProperty* Property) const
{
	bool bIsDelegate = Property->IsA<FMulticastSparseDelegateProperty>();
	return bIsDelegate;
}

FString FMulticastDelegatePropertyTranslator::GetManagedType(const FProperty* Property) const
{
	return "";
}

FString FMulticastDelegatePropertyTranslator::GetNullReturnCSharpValue(const FProperty* ReturnProperty) const
{
	return "null";
}
