#include "MulticastDelegatePropertyTranslator.h"

#include "GlueGenerator/CSGenerator.h"
#include "GlueGenerator/CSScriptBuilder.h"

bool FMulticastDelegatePropertyTranslator::CanHandleProperty(const FProperty* Property) const
{
	const FMulticastDelegateProperty* DelegateProperty = CastField<FMulticastDelegateProperty>(Property);

	if (!FCSGenerator::Get().CanExportFunctionParameters(DelegateProperty->SignatureFunction))
	{
		return false;	
	}
	
	return true;
}

void FMulticastDelegatePropertyTranslator::AddDelegateReferences(const FProperty* Property, TSet<UFunction*>& DelegateSignatures) const
{
	const FMulticastDelegateProperty* DelegateProperty = CastField<FMulticastDelegateProperty>(Property);
	DelegateSignatures.Add(DelegateProperty->SignatureFunction);
}

FString FMulticastDelegatePropertyTranslator::GetManagedType(const FProperty* Property) const
{
	return GetDelegateName(CastFieldChecked<FMulticastDelegateProperty>(Property));
}

void FMulticastDelegatePropertyTranslator::ExportPropertyStaticConstruction(FCSScriptBuilder& Builder, const FProperty* Property, const FString& NativePropertyName) const
{
	FDelegateBasePropertyTranslator::ExportPropertyStaticConstruction(Builder, Property, NativePropertyName);

	const FMulticastDelegateProperty* DelegateProperty = CastFieldChecked<FMulticastDelegateProperty>(Property);

	if (DelegateProperty->SignatureFunction->NumParms > 0)
	{
		FString DelegateName = GetDelegateName(DelegateProperty);
		Builder.AppendLine(FString::Printf(TEXT("%s.InitializeUnrealDelegate(%s_NativeProperty);"), *DelegateName, *NativePropertyName));
	}
}

void FMulticastDelegatePropertyTranslator::ExportPropertyVariables(FCSScriptBuilder& Builder, const FProperty* Property, const FString& PropertyName) const
{
	AddNativePropertyField(Builder, PropertyName);

	FString BackingFieldName = GetBackingFieldName(Property);
	FString DelegateName = GetDelegateName(CastFieldChecked<FMulticastDelegateProperty>(Property));
	Builder.AppendLine(FString::Printf(TEXT("private %s %s;"), *DelegateName, *BackingFieldName));
	
	FPropertyTranslator::ExportPropertyVariables(Builder, Property, PropertyName);
}

void FMulticastDelegatePropertyTranslator::ExportPropertySetter(FCSScriptBuilder& Builder, const FProperty* Property, const FString& PropertyName) const
{
	FString BackingFieldName = GetBackingFieldName(Property);
	FString DelegateName = GetDelegateName(CastFieldChecked<FMulticastDelegateProperty>(Property));

	Builder.AppendLine(FString::Printf(TEXT("if (value == %s)"), *BackingFieldName));
	Builder.OpenBrace();
	Builder.AppendLine("return;");
	Builder.CloseBrace();
	Builder.AppendLine(FString::Printf(TEXT("%s = value;"), *BackingFieldName));
	Builder.AppendLine(FString::Printf(TEXT("DelegateMarshaller<%s>.ToNative(IntPtr.Add(NativeObject,%s_Offset),0,this,value);"), *DelegateName, *PropertyName));
}

void FMulticastDelegatePropertyTranslator::ExportPropertyGetter(FCSScriptBuilder& Builder, const FProperty* Property, const FString& PropertyName) const
{
	FString BackingFieldName = GetBackingFieldName(Property);
	FString NativePropertyFieldName = GetNativePropertyField(PropertyName);
	FString DelegateName = GetDelegateName(CastFieldChecked<FMulticastDelegateProperty>(Property));
	
	Builder.AppendLine(FString::Printf(TEXT("if (%s == null)"), *BackingFieldName));
	Builder.OpenBrace();
	Builder.AppendLine(FString::Printf(TEXT("%s = DelegateMarshaller<%s>.FromNative(IntPtr.Add(NativeObject, %s_Offset), %s, 0, this);"),
		*BackingFieldName, GetData(DelegateName), GetData(PropertyName), *NativePropertyFieldName));
	Builder.CloseBrace();
	Builder.AppendLine(FString::Printf(TEXT("return %s;"), *BackingFieldName));
}

FString FMulticastDelegatePropertyTranslator::GetNullReturnCSharpValue(const FProperty* ReturnProperty) const
{
	return "null";
}

FString FMulticastDelegatePropertyTranslator::GetBackingFieldName(const FProperty* Property)
{
	return FString::Printf(TEXT("%s_BackingField"), *Property->GetName());
}

FString FMulticastDelegatePropertyTranslator::GetDelegateName(const FMulticastDelegateProperty* Property)
{
	const UFunction* Function = Property->SignatureFunction;
	return FDelegateBasePropertyTranslator::GetDelegateName(Function);
}