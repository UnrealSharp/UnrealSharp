#include "BitfieldPropertyTranslator.h"
#include "GlueGenerator/CSScriptBuilder.h"
#include "GlueGenerator/CSPropertyTranslatorManager.h"

FBitfieldPropertyTranslator::FBitfieldPropertyTranslator(FCSPropertyTranslatorManager& InPropertyHandlers)
: FPropertyTranslator(InPropertyHandlers, static_cast<EPropertyUsage>(EPU_Any & ~EPU_StaticArrayProperty))
{

}

void FBitfieldPropertyTranslator::ExportPropertyStaticConstruction(FCSScriptBuilder& Builder, const FProperty* Property, const FString& NativePropertyName) const
{
	FPropertyTranslator::ExportPropertyStaticConstruction(Builder, Property, NativePropertyName);
	Builder.AppendLine(FString::Printf(TEXT("%s_NativeProperty = %s.CallGetNativePropertyFromName(NativeClassPtr, \"%s\");"), *NativePropertyName, FPropertyCallbacks, *NativePropertyName));
}
bool FBitfieldPropertyTranslator::CanHandleProperty(const FProperty* Property) const
{
	const FBoolProperty* BoolProperty = CastFieldChecked<FBoolProperty>(Property);
	return !BoolProperty->IsNativeBool();
}

FString FBitfieldPropertyTranslator::GetManagedType(const FProperty* Property) const
{
	return "bool";
}

void FBitfieldPropertyTranslator::ExportPropertyVariables(FCSScriptBuilder& Builder, const FProperty* Property, const FString& NativePropertyName) const
{
	FPropertyTranslator::ExportPropertyVariables(Builder, Property, NativePropertyName);
	Builder.AppendLine(FString::Printf(TEXT("static readonly IntPtr %s_NativeProperty;"), *NativePropertyName));
}

void FBitfieldPropertyTranslator::ExportMarshalFromNativeBuffer(FCSScriptBuilder& Builder, const FProperty* Property, const FString &Owner, const FString& NativePropertyName, const FString& AssignmentOrReturn, const FString& SourceBuffer, const FString& Offset, bool bCleanupSourceBuffer, bool reuseRefMarshallers) const
{
	Builder.AppendLine(FString::Printf(TEXT("%s %s.CallGetBitfieldValueFromProperty(%s, %s_NativeProperty, %s);"), *AssignmentOrReturn, FBoolPropertyCallbacks, *SourceBuffer, *NativePropertyName, *Offset));
}

void FBitfieldPropertyTranslator::ExportCleanupMarshallingBuffer(FCSScriptBuilder& Builder, const FProperty* ParamProperty, const FString& ParamName) const
{

}

void FBitfieldPropertyTranslator::ExportMarshalToNativeBuffer(FCSScriptBuilder& Builder, const FProperty* Property, const FString &Owner, const FString& NativePropertyName, const FString& DestinationBuffer, const FString& Offset, const FString& Source) const
{
	Builder.AppendLine(FString::Printf(TEXT("%s.CallSetBitfieldValueForProperty(%s, %s_NativeProperty, %s, %s);"), FBoolPropertyCallbacks, *DestinationBuffer, *NativePropertyName, *Offset, *Source));
}

FString FBitfieldPropertyTranslator::GetNullReturnCSharpValue(const FProperty* ReturnProperty) const
{
	return "false";
}
