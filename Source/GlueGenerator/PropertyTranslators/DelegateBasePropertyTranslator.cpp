#include "DelegateBasePropertyTranslator.h"
#include "GlueGenerator/CSScriptBuilder.h"

void FDelegateBasePropertyTranslator::ExportPropertyStaticConstruction(FCSScriptBuilder& Builder,const FProperty* Property, const FString& NativePropertyName) const
{
	Builder.AppendLine(FString::Printf(TEXT("%s_NativeProperty = %s.CallGetNativePropertyFromName(NativeClassPtr, \"%s\");"), *NativePropertyName, FPropertyCallbacks, *NativePropertyName));
	FPropertyTranslator::ExportPropertyStaticConstruction(Builder, Property, NativePropertyName);
}

FString FDelegateBasePropertyTranslator::GetDelegateName(const UFunction* SignatureFunction)
{
	FString DelegateSignatureName = SignatureFunction->GetName();
	int32 DelegateSignatureIndex = DelegateSignatureName.Find("__DelegateSignature");
	return DelegateSignatureName.Left(DelegateSignatureIndex);
}
