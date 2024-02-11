#include "DelegateBasePropertyTranslator.h"

#include "GlueGenerator/CSScriptBuilder.h"

void FDelegateBasePropertyTranslator::ExportPropertyStaticConstruction(FCSScriptBuilder& Builder,const FProperty* Property, const FString& NativePropertyName) const
{
	Builder.AppendLine(FString::Printf(TEXT("%s_NativeProperty = %s.CallGetNativePropertyFromName(NativeClassPtr, \"%s\");"), *NativePropertyName, FPropertyCallbacks, *NativePropertyName));
	FPropertyTranslator::ExportPropertyStaticConstruction(Builder, Property, NativePropertyName);

	const FMulticastDelegateProperty* DelegateProperty = CastFieldChecked<FMulticastDelegateProperty>(Property);
	
	if (DelegateProperty->SignatureFunction->NumParms > 0)
	{
		Builder.AppendLine(FString::Printf(TEXT("%s.InitializeUnrealDelegate(%s_NativeProperty);"), *NativePropertyName, *NativePropertyName));
	}
}
