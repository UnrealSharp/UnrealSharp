#include "StructPropertyTranslator.h"

FStructPropertyTranslator::FStructPropertyTranslator(FCSPropertyTranslatorManager& InPropertyHandlers)
: FSimpleTypePropertyTranslator(InPropertyHandlers, FStructProperty::StaticClass(), EPU_Any)
{
	
}

FString FStructPropertyTranslator::GetManagedType(const FProperty* Property) const
{
	const FStructProperty* StructProperty = CastFieldChecked<FStructProperty>(Property);
	check(StructProperty->Struct);
	return GetScriptNameMapper().GetQualifiedName(StructProperty->Struct);
}

void FStructPropertyTranslator::AddReferences(const FProperty* Property, TSet<UField*>& References) const
{
	const FStructProperty* StructProperty = CastFieldChecked<FStructProperty>(Property);
	References.Add(StructProperty->Struct);
}

FString FStructPropertyTranslator::GetMarshaller(const FProperty *Property) const
{
	return FString::Printf(TEXT("%sMarshaller"), *GetManagedType(Property));
}

void FStructPropertyTranslator::ExportCppDefaultParameterAsLocalVariable(FCSScriptBuilder& Builder, const FString& VariableName, const FString& CppDefaultValue, UFunction* Function, FProperty* ParamProperty) const
{
	ExportDefaultStructParameter(Builder, VariableName, CppDefaultValue, ParamProperty, *this);
}