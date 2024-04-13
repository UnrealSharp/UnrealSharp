#pragma once
#include "PropertyTranslator.h"

class FCSPropertyTranslatorManager;

class FBitfieldPropertyTranslator : public FPropertyTranslator
{
public:
	explicit FBitfieldPropertyTranslator(FCSPropertyTranslatorManager& InPropertyHandlers);

	//FPropertyTranslator interface implementation
	virtual bool CanHandleProperty(const FProperty* Property) const override;
	virtual FString GetManagedType(const FProperty* Property) const override;
	virtual void ExportPropertyStaticConstruction(FCSScriptBuilder& Builder, const FProperty* Property, const FString& NativePropertyName) const override;
protected:
	virtual void ExportPropertyVariables(FCSScriptBuilder& Builder, const FProperty* Property, const FString& PropertyName) const override;
	virtual FString GetNullReturnCSharpValue(const FProperty* ReturnProperty) const override;
	virtual void ExportMarshalFromNativeBuffer(FCSScriptBuilder& Builder, const FProperty* Property, const FString &Owner, const FString& PropertyName, const FString& AssignmentOrReturn, const FString& SourceBuffer, const FString& Offset, bool bCleanupSourceBuffer, bool reuseRefMarshallers) const override;
	virtual void ExportCleanupMarshallingBuffer(FCSScriptBuilder& Builder, const FProperty* ParamProperty, const FString& ParamName) const override;
	virtual void ExportMarshalToNativeBuffer(FCSScriptBuilder& Builder, const FProperty* Property, const FString &Owner, const FString& PropertyName, const FString& DestinationBuffer, const FString& Offset, const FString& Source) const override;
	//End of implementation
};
