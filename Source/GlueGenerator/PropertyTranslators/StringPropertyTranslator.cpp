#include "StringPropertyTranslator.h"
#include "GlueGenerator/CSScriptBuilder.h"
#include "GlueGenerator/CSPropertyTranslatorManager.h"

FStringPropertyTranslator::FStringPropertyTranslator(FCSPropertyTranslatorManager& InPropertyHandlers)
: FPropertyTranslator(InPropertyHandlers, static_cast<EPropertyUsage>(EPU_Property | EPU_Parameter | EPU_ReturnValue | EPU_OverridableFunctionParameter | EPU_OverridableFunctionReturnValue | EPU_StaticArrayProperty))
{

}

bool FStringPropertyTranslator::CanHandleProperty(const FProperty* Property) const
{
	return true;
}

FString FStringPropertyTranslator::GetManagedType(const FProperty* Property) const
{
	return "string";
}

void FStringPropertyTranslator::ExportPropertyStaticConstruction(FCSScriptBuilder& Builder, const FProperty* Property, const FString& NativePropertyName) const
{
	FPropertyTranslator::ExportPropertyStaticConstruction(Builder, Property, NativePropertyName);
	Builder.AppendLine(FString::Printf(TEXT("%s_NativeProperty = %s.CallGetNativePropertyFromName(NativeClassPtr, \"%s\");"), *NativePropertyName, FPropertyCallbacks, *NativePropertyName));
}

void FStringPropertyTranslator::ExportPropertyVariables(FCSScriptBuilder& Builder, const FProperty* Property, const FString& NativePropertyName) const
{
	FPropertyTranslator::ExportPropertyVariables(Builder, Property, NativePropertyName);
	Builder.AppendLine(FString::Printf(TEXT("static readonly IntPtr %s_NativeProperty;"), *NativePropertyName));
}

void FStringPropertyTranslator::ExportPropertySetter(FCSScriptBuilder& Builder, const FProperty* Property, const FString& NativePropertyName) const
{
	AddCheckObjectForValidity(Builder);
	Builder.AppendLine(FString::Printf(TEXT("StringMarshaller.ToNative(IntPtr.Add(NativeObject,%s_Offset),0,this,value);"),*NativePropertyName));
}


void FStringPropertyTranslator::ExportPropertyGetter(FCSScriptBuilder& Builder, const FProperty* Property, const FString& NativePropertyName) const
{
	AddCheckObjectForValidity(Builder);
	Builder.AppendLine(FString::Printf(TEXT("return StringMarshaller.FromNative(IntPtr.Add(NativeObject,%s_Offset),0,this);"), *NativePropertyName));
}


void FStringPropertyTranslator::ExportFunctionReturnStatement(FCSScriptBuilder& Builder, const UFunction* Function, const FProperty* ReturnProperty, const FString& FunctionName, const FString& ParamsCallString) const
{
	Builder.AppendLine(FString::Printf(TEXT("return %s.CallConvertTCHARToUTF8(Invoke_%s(NativeObject, %s_NativeFunction%s));"), FStringCallbacks, *FunctionName, *FunctionName, *ParamsCallString));
}

FString FStringPropertyTranslator::GetNullReturnCSharpValue(const FProperty* ReturnProperty) const
{
	//we can't use string.empty as this may be used for places where it must be a compile-time constant
	return "\"\"";
}

void FStringPropertyTranslator::ExportMarshalToNativeBuffer(FCSScriptBuilder& Builder, const FProperty* Property, const FString &Owner, const FString& NativePropertyName, const FString& DestinationBuffer, const FString& Offset, const FString& Source) const
{
	Builder.AppendLine(FString::Printf(TEXT("IntPtr %s_NativePtr = IntPtr.Add(%s,%s);"), *NativePropertyName, *DestinationBuffer, *Offset));
	Builder.AppendLine(FString::Printf(TEXT("StringMarshaller.ToNative(%s_NativePtr,0,%s,%s);"),*NativePropertyName, *Owner, *Source));
}

void FStringPropertyTranslator::ExportCleanupMarshallingBuffer(FCSScriptBuilder& Builder, const FProperty* ParamProperty, const FString& ParamName) const
{
	Builder.AppendLine(FString::Printf(TEXT("StringMarshaller.DestructInstance(%s_NativePtr, 0);"), *ParamName));
}

void FStringPropertyTranslator::ExportMarshalFromNativeBuffer(FCSScriptBuilder& Builder, const FProperty* Property, const FString &Owner, const FString& NativePropertyName, const FString& AssignmentOrReturn, const FString& SourceBuffer, const FString& Offset, bool bCleanupSourceBuffer, bool reuseRefMarshallers) const
{
	//if it was a "ref" parameter, we set this pointer up before calling the function. if not, create one.
	if (!reuseRefMarshallers)
	{
		Builder.AppendLine(FString::Printf(TEXT("IntPtr %s_NativePtr = IntPtr.Add(%s,%s);"), *NativePropertyName, *SourceBuffer, *Offset));
	}
	// The mirror struct references a temp string buffer which we must clean up.
	Builder.AppendLine(FString::Printf(TEXT("%s StringMarshaller.FromNative(%s_NativePtr,0,%s);"),*AssignmentOrReturn, *NativePropertyName, *Owner));
	if (bCleanupSourceBuffer)
	{
		// Ensure we're not generating unreachable cleanup code.
		check(AssignmentOrReturn != "return");
		Builder.AppendLine(FString::Printf(TEXT("StringMarshaller.DestructInstance(%s_NativePtr, 0);"), *NativePropertyName));
	}
}

FString FStringPropertyTranslator::ExportMarshallerDelegates(const FProperty *Property, const FString &NativePropertyName) const
{
	return TEXT("StringMarshaller.ToNative, StringMarshaller.FromNative");
}

FString FStringPropertyTranslator::ConvertCppDefaultParameterToCSharp(const FString& CppDefaultValue, UFunction* Function, FProperty* ParamProperty) const
{
	return "\"" + CppDefaultValue + "\"";
}