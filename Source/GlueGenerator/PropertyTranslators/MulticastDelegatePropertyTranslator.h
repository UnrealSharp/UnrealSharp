#pragma once
#include "PropertyTranslator.h"

class FMulticastDelegatePropertyTranslator : public FPropertyTranslator
{
public:
	FMulticastDelegatePropertyTranslator(FCSSupportedPropertyTranslators& InPropertyHandlers) : FPropertyTranslator(InPropertyHandlers, EPU_Property)
	{
	}

	//FPropertyTranslator interface implementation
	virtual bool CanHandleProperty(const FProperty* Property) const override;
	virtual FString GetManagedType(const FProperty* Property) const override;
	virtual void ExportPropertyStaticConstruction(FCSScriptBuilder& Builder, const FProperty* Property, const FString& NativePropertyName) const override;
protected:
	virtual void ExportPropertyVariables(FCSScriptBuilder& Builder, const FProperty* Property, const FString& PropertyName) const override;
	virtual void ExportPropertySetter(FCSScriptBuilder& Builder, const FProperty* Property, const FString& PropertyName) const override;
	virtual void ExportPropertyGetter(FCSScriptBuilder& Builder, const FProperty* Property, const FString& PropertyName) const override;
	virtual FString GetNullReturnCSharpValue(const FProperty* ReturnProperty) const override;
	virtual void OnPropertyExported(FCSScriptBuilder& Builder, const FProperty* Property, const FString& PropertyName) const override;
	//End of implementation

	static FString GetBackingFieldName(const FProperty* Property);

	TSet<FName> ExportedDelegates;
};
