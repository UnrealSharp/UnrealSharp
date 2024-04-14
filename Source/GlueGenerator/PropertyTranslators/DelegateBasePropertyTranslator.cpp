#include "DelegateBasePropertyTranslator.h"
#include "GlueGenerator/CSScriptBuilder.h"

void FDelegateBasePropertyTranslator::ExportPropertyStaticConstruction(FCSScriptBuilder& Builder,const FProperty* Property, const FString& NativePropertyName) const
{
	Builder.AppendLine(FString::Printf(TEXT("%s_NativeProperty = %s.CallGetNativePropertyFromName(NativeClassPtr, \"%s\");"), *NativePropertyName, FPropertyCallbacks, *NativePropertyName));
	FPropertyTranslator::ExportPropertyStaticConstruction(Builder, Property, NativePropertyName);

	const FMulticastDelegateProperty* DelegateProperty = CastFieldChecked<FMulticastDelegateProperty>(Property);
	
	if (DelegateProperty->SignatureFunction->NumParms > 0)
	{
		FString DelegateName = GetDelegateName(DelegateProperty);
		Builder.AppendLine(FString::Printf(TEXT("%s.InitializeUnrealDelegate(%s_NativeProperty);"), *DelegateName, *NativePropertyName));
	}
}

FString FDelegateBasePropertyTranslator::GetDelegateName(const FMulticastDelegateProperty* Property)
{
	UFunction* Function = Property->SignatureFunction;
	FString DelegateSignatureName = Function->GetName();
	int32 DelegateSignatureIndex = DelegateSignatureName.Find("DelegateSignature");
	return DelegateSignatureName.Left(DelegateSignatureIndex - 2);
}
