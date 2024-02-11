#include "SingleDelegatePropertyTranslator.h"

#include "GlueGenerator/CSGenerator.h"

bool FSingleDelegatePropertyTranslator::CanHandleProperty(const FProperty* Property) const
{
	const FDelegateProperty* DelegateProperty = CastField<FDelegateProperty>(Property);

	if (!FCSGenerator::Get().CanExportFunctionParameters(DelegateProperty->SignatureFunction))
	{
		return false;	
	}
	
	return true;
}

void FSingleDelegatePropertyTranslator::ExportParameterStaticConstruction(FCSScriptBuilder& Builder,const FString& NativeMethodName, const FProperty* Parameter) const
{
	FDelegateBasePropertyTranslator::ExportParameterStaticConstruction(Builder, NativeMethodName, Parameter);
}

void FSingleDelegatePropertyTranslator::ExportPropertyVariables(FCSScriptBuilder& Builder, const FProperty* Property, const FString& PropertyName) const
{
	FPropertyTranslator::ExportPropertyVariables(Builder, Property, PropertyName);
}

void FSingleDelegatePropertyTranslator::ExportPropertySetter(FCSScriptBuilder& Builder, const FProperty* Property, const FString& PropertyName) const
{
	FPropertyTranslator::ExportPropertySetter(Builder, Property, PropertyName);
}

void FSingleDelegatePropertyTranslator::ExportPropertyGetter(FCSScriptBuilder& Builder, const FProperty* Property, const FString& PropertyName) const
{
	FPropertyTranslator::ExportPropertyGetter(Builder, Property, PropertyName);
}

FString FSingleDelegatePropertyTranslator::GetNullReturnCSharpValue(const FProperty* ReturnProperty) const
{
	return "";
}

void FSingleDelegatePropertyTranslator::OnPropertyExported(FCSScriptBuilder& Builder, const FProperty* Property, const FString& PropertyName) const
{
	FPropertyTranslator::OnPropertyExported(Builder, Property, PropertyName);
}
