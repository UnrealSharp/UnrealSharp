#include "TextPropertyTranslator.h"

#include "GlueGenerator/CSScriptBuilder.h"

FTextPropertyTranslator::FTextPropertyTranslator(FCSSupportedPropertyTranslators& InPropertyHandlers)
: FPropertyTranslator(InPropertyHandlers, static_cast<EPropertyUsage>(EPU_Property | EPU_StaticArrayProperty))
{
	
}

bool FTextPropertyTranslator::CanHandleProperty(const FProperty* Property) const
{
	check(Property->IsA(FTextProperty::StaticClass()));
	return true;
}

FString FTextPropertyTranslator::GetManagedType(const FProperty* Property) const
{
	return TEXT("Text");
}

void FTextPropertyTranslator::ExportPropertyStaticConstruction(FCSScriptBuilder& Builder, const FProperty* Property, const FString& NativePropertyName) const
{
	FPropertyTranslator::ExportPropertyStaticConstruction(Builder, Property, NativePropertyName);
	Builder.AppendLine(FString::Printf(TEXT("%s_NativeProperty = %s.CallGetNativePropertyFromName(NativeClassPtr, \"%s\");"), *NativePropertyName, FPropertyCallbacks, *NativePropertyName));
}

void FTextPropertyTranslator::ExportPropertyVariables(FCSScriptBuilder& Builder, const FProperty* Property, const FString& NativePropertyName) const
{
	FPropertyTranslator::ExportPropertyVariables(Builder, Property, NativePropertyName);
	Builder.AppendLine(FString::Printf(TEXT("static readonly IntPtr %s_NativeProperty;"), *NativePropertyName));
	if (Property->ArrayDim == 1)
	{
		Builder.AppendLine(FString::Printf(TEXT("TextMarshaller %s_Wrapper;"), *NativePropertyName));
	}
}

void FTextPropertyTranslator::ExportPropertyGetter(FCSScriptBuilder& Builder, const FProperty* Property, const FString& NativePropertyName) const
{
	Builder.AppendLine(FString::Printf(TEXT("if (%s_Wrapper == null)"), *NativePropertyName));
	Builder.OpenBrace();
	check(Property->ArrayDim == 1);
	Builder.AppendLine(FString::Printf(TEXT("%s_Wrapper  = new TextMarshaller(1);"), *NativePropertyName));
	Builder.CloseBrace();
	Builder.AppendLine(FString::Printf(TEXT("return %s_Wrapper.FromNative(this.NativeObject + %s_Offset, 0, this);"), *NativePropertyName, *NativePropertyName));
}

FString FTextPropertyTranslator::GetNullReturnCSharpValue(const FProperty* ReturnProperty) const
{
	return TEXT("null");
}

FString FTextPropertyTranslator::ExportInstanceMarshallerVariables(const FProperty *Property, const FString &PropertyName) const
{
	return FString::Printf(TEXT("TextMarshaller InstanceMarshaller = new TextMarshaller(%s_Length);"), *PropertyName);
}

FString FTextPropertyTranslator::ExportMarshallerDelegates(const FProperty *Property, const FString &PropertyName) const
{
	return "InstanceMarshaller.ToNative, InstanceMarshaller.FromNative";
}
