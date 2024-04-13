#include "CustomStructTypePropertyTranslator.h"

FCustomStructTypePropertyTranslator::FCustomStructTypePropertyTranslator(
	FCSPropertyTranslatorManager& InPropertyHandlers, const FString& InUnrealStructName,
	const FString& InCSharpStructName)
: FSimpleTypePropertyTranslator(InPropertyHandlers, FStructProperty::StaticClass(), InCSharpStructName), UnrealStructName(InUnrealStructName)
{

}

bool FCustomStructTypePropertyTranslator::CanHandleProperty(const FProperty* Property) const
{
	const FStructProperty* StructProperty = CastFieldChecked<FStructProperty>(Property);
	return StructProperty->Struct->GetFName() == UnrealStructName;
}

void FCustomStructTypePropertyTranslator::AddReferences(const FProperty* Property, TSet<UField*>& References) const
{
	// Do nothing - we're just hiding the base class version, which would export a default version
	// of the property's struct.
}

FString FCustomStructTypePropertyTranslator::GetMarshaller(const FProperty *Property) const
{
	return FString::Printf(TEXT("%sMarshaller"), *GetManagedType(Property));
}

void FCustomStructTypePropertyTranslator::ExportCppDefaultParameterAsLocalVariable(FCSScriptBuilder& Builder, const FString& VariableName, const FString& CppDefaultValue, UFunction* Function, FProperty* ParamProperty) const
{
	ExportDefaultStructParameter(Builder, VariableName, CppDefaultValue, ParamProperty, *this);
}
