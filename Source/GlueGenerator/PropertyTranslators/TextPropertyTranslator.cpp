#include "TextPropertyTranslator.h"

#include "GlueGenerator/CSScriptBuilder.h"

FTextPropertyTranslator::FTextPropertyTranslator(FCSPropertyTranslatorManager& InPropertyHandlers)
: FBlittableTypePropertyTranslator(InPropertyHandlers, FTextProperty::StaticClass(), "Text")
{
	
}

FString FTextPropertyTranslator::GetNullReturnCSharpValue(const FProperty* ReturnProperty) const
{
	return "default(Text)";
}

void FTextPropertyTranslator::ExportCppDefaultParameterAsLocalVariable(FCSScriptBuilder& Builder, const FString& VariableName, const FString& CppDefaultValue, UFunction* Function, FProperty* ParamProperty) const
{
	Builder.AppendLine(FString::Printf(TEXT("Text %s = Text.None;"), *VariableName));
}

FString FTextPropertyTranslator::GetMarshaller(const FProperty* Property) const
{
	return "TextMarshaller";
}
