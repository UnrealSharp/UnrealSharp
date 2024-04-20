#include "SinglecastDelegatePropertyTranslator.h"

#include "GlueGenerator/CSGenerator.h"
#include "GlueGenerator/CSScriptBuilder.h"

bool FSinglecastDelegatePropertyTranslator::CanHandleProperty(const FProperty* Property) const
{
	const FDelegateProperty* DelegateProperty = CastField<FDelegateProperty>(Property);

	if (!FCSGenerator::Get().CanExportFunctionParameters(DelegateProperty->SignatureFunction))
	{
		return false;	
	}
	
	return true;
}

void FSinglecastDelegatePropertyTranslator::AddDelegateReferences(const FProperty* Property, TSet<UFunction*>& DelegateSignatures) const
{
	const FDelegateProperty* DelegateProperty = CastField<FDelegateProperty>(Property);
	DelegateSignatures.Add(DelegateProperty->SignatureFunction);
}

FString FSinglecastDelegatePropertyTranslator::GetManagedType(const FProperty* Property) const
{
	return GetDelegateName(CastFieldChecked<FDelegateProperty>(Property));
}

void FSinglecastDelegatePropertyTranslator::ExportPropertyStaticConstruction(FCSScriptBuilder& Builder, const FProperty* Property, const FString& NativePropertyName) const
{
	FDelegateBasePropertyTranslator::ExportPropertyStaticConstruction(Builder, Property, NativePropertyName);

	const FDelegateProperty* DelegateProperty = CastFieldChecked<FDelegateProperty>(Property);

	if (DelegateProperty->SignatureFunction->NumParms > 0)
	{
		FString DelegateName = GetDelegateName(DelegateProperty);
		Builder.AppendLine(FString::Printf(TEXT("%s.InitializeUnrealDelegate(%s_NativeProperty);"), *DelegateName, *NativePropertyName));
	}
}

void FSinglecastDelegatePropertyTranslator::ExportMarshalToNativeBuffer(FCSScriptBuilder& Builder, const FProperty* Property, const FString& Owner, const FString& PropertyName, const FString& DestinationBuffer, const FString& Offset, const FString& Source) const
{
	FString DelegateName = GetDelegateName(CastFieldChecked<FDelegateProperty>(Property));
	Builder.AppendLine(FString::Printf(TEXT("DelegateMarshaller<%s>.ToNative(IntPtr.Add(%s, %s), 0, %s, %s);"), *DelegateName, *DestinationBuffer, *Offset, *Owner, *Source));
}

void FSinglecastDelegatePropertyTranslator::ExportCleanupMarshallingBuffer(FCSScriptBuilder& Builder, const FProperty* ParamProperty, const FString& NativeParamName) const
{
	
}

void FSinglecastDelegatePropertyTranslator::ExportMarshalFromNativeBuffer(FCSScriptBuilder& Builder, const FProperty* Property, const FString& Owner, const FString& PropertyName, const FString& AssignmentOrReturn, const FString& SourceBuffer, const FString& Offset, bool bCleanupSourceBuffer, bool reuseRefMarshallers) const
{
	FString DelegateName = GetDelegateName(CastFieldChecked<FDelegateProperty>(Property));
	Builder.AppendLine(FString::Printf(TEXT("%s DelegateMarshaller<%s>.FromNative(IntPtr.Add(%s, %s), IntPtr.Zero, 0, %s);"), *AssignmentOrReturn, *DelegateName, *SourceBuffer, *Offset, *Owner));
}

FString FSinglecastDelegatePropertyTranslator::GetNullReturnCSharpValue(const FProperty* ReturnProperty) const
{
	return "null";
}

FString FSinglecastDelegatePropertyTranslator::GetDelegateName(const FDelegateProperty* Property)
{
	const UFunction* Function = Property->SignatureFunction;
	return FDelegateBasePropertyTranslator::GetDelegateName(Function);
}