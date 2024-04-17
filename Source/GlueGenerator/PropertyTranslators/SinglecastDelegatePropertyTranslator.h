#pragma once

#include "DelegateBasePropertyTranslator.h"
#include "PropertyTranslator.h"

class FSinglecastDelegatePropertyTranslator : public FDelegateBasePropertyTranslator
{
public:
	
	FSinglecastDelegatePropertyTranslator(FCSPropertyTranslatorManager& InPropertyHandlers)
	: FDelegateBasePropertyTranslator(InPropertyHandlers, EPU_Parameter)
	{
	}

	//FPropertyTranslator interface implementation
	virtual bool CanHandleProperty(const FProperty* Property) const override;
	virtual void AddDelegateReferences(const FProperty* Property, TSet<UFunction*>& DelegateSignatures) const override;
	virtual FString GetManagedType(const FProperty* Property) const override;
	virtual void ExportPropertyStaticConstruction(FCSScriptBuilder& Builder,
		const FProperty* Property,
		const FString& NativePropertyName) const override;
protected:
	virtual void ExportMarshalToNativeBuffer(FCSScriptBuilder& Builder, const FProperty* Property, const FString& Owner, const FString& PropertyName, const FString& DestinationBuffer, const FString& Offset, const FString& Source) const override;
	virtual void ExportCleanupMarshallingBuffer(FCSScriptBuilder& Builder, const FProperty* ParamProperty, const FString& NativeParamName) const override;
	virtual void ExportMarshalFromNativeBuffer(FCSScriptBuilder& Builder, const FProperty* Property, const FString& Owner, const FString& PropertyName, const FString& AssignmentOrReturn, const FString& SourceBuffer, const FString& Offset, bool bCleanupSourceBuffer, bool reuseRefMarshallers) const override;
	virtual FString GetNullReturnCSharpValue(const FProperty* ReturnProperty) const override;
	//End of implementation

	static FString GetDelegateName(const FDelegateProperty* Property);
};
