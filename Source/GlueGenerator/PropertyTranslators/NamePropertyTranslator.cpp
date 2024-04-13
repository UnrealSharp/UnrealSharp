#include "NamePropertyTranslator.h"
#include "GlueGenerator/CSScriptBuilder.h"

FNamePropertyTranslator::FNamePropertyTranslator(FCSPropertyTranslatorManager& InPropertyHandlers)
: FBlittableTypePropertyTranslator(InPropertyHandlers, FNameProperty::StaticClass(), TEXT("Name"))
{
	
}

FString FNamePropertyTranslator::GetNullReturnCSharpValue(const FProperty* ReturnProperty) const
{
	return "default(Name)";
}

void FNamePropertyTranslator::ExportCppDefaultParameterAsLocalVariable(FCSScriptBuilder& Builder, const FString& VariableName, const FString& CppDefaultValue, UFunction* Function, FProperty* ParamProperty) const
{
	if (CppDefaultValue == "None")
	{
		Builder.AppendLine(FString::Printf(TEXT("Name %s = Name.None;"), *VariableName));
	}
	else
	{
		Builder.AppendLine(FString::Printf(TEXT("Name %s = new Name(\"%s\");"), *VariableName, *CppDefaultValue));
	}
}