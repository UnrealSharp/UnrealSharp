#include "NullPropertyTranslator.h"
#include "GlueGenerator/CSPropertyTranslatorManager.h"

FNullPropertyTranslator::FNullPropertyTranslator(FCSPropertyTranslatorManager& InPropertyHandlers)
: FPropertyTranslator(InPropertyHandlers, EPU_None)
{
	
}

bool FNullPropertyTranslator::CanHandleProperty(const FProperty* Property) const
{
	return true;
}

FString FNullPropertyTranslator::GetManagedType(const FProperty* Property) const
{
	// In general, the NULL handler should be a no-op, but we need to return a useful value for function
	// return properties to ensure void method signatures are generated correctly.
	return "void";
}

FString FNullPropertyTranslator::GetNullReturnCSharpValue(const FProperty* ReturnProperty) const
{
	checkNoEntry();
	return "";
}
