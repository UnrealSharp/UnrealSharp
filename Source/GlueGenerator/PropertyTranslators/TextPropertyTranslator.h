#pragma once
#include "PropertyTranslator.h"

class FTextPropertyTranslator : public FPropertyTranslator
{
public:
	
	explicit FTextPropertyTranslator(FCSSupportedPropertyTranslators& InPropertyHandlers);

	// FPropertyTranslator interface implementation
	virtual bool CanHandleProperty(const FProperty* Property) const override;
	virtual FString GetManagedType(const FProperty* Property) const override;
	virtual void ExportPropertyStaticConstruction(FCSScriptBuilder& Builder, const FProperty* Property, const FString& NativePropertyName) const override;
	virtual FString ExportInstanceMarshallerVariables(const FProperty *Property, const FString &PropertyName) const override;
	virtual FString ExportMarshallerDelegates(const FProperty *Property, const FString &PropertyName) const override;
protected:
	virtual void ExportPropertyGetter(FCSScriptBuilder& Builder, const FProperty* Property, const FString& PropertyName) const override;
	virtual bool IsSetterRequired() const override { return false; }
	virtual void ExportPropertyVariables(FCSScriptBuilder& Builder, const FProperty* Property, const FString& PropertyName) const override;
	virtual FString GetNullReturnCSharpValue(const FProperty* ReturnProperty) const override;
	// End of implementation


};

