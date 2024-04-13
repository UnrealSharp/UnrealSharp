#include "BlittableCustomStructTypePropertyTranslator.h"

FBlittableCustomStructTypePropertyTranslator::FBlittableCustomStructTypePropertyTranslator
(FCSPropertyTranslatorManager& InPropertyHandlers, const FString& InUnrealStructName, const FString& CSharpStructName)
: FBlittableTypePropertyTranslator(InPropertyHandlers, FStructProperty::StaticClass(), CSharpStructName, EPU_Any), UnrealStructName(InUnrealStructName)
{
}

bool FBlittableCustomStructTypePropertyTranslator::CanHandleProperty(const FProperty* Property) const
{
	const FStructProperty& StructProperty = *CastFieldChecked<FStructProperty>(Property);
	return StructProperty.Struct->GetFName() == UnrealStructName;
}

void FBlittableCustomStructTypePropertyTranslator::ExportCppDefaultParameterAsLocalVariable(FCSScriptBuilder& Builder, const FString& VariableName, const FString& CppDefaultValue, UFunction* Function, FProperty* ParamProperty) const
{
	ExportDefaultStructParameter(Builder, VariableName, CppDefaultValue, ParamProperty, *this);
}