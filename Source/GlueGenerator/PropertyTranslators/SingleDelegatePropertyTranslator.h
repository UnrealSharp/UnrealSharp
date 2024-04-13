#pragma once
#include "DelegateBasePropertyTranslator.h"
#include "PropertyTranslator.h"

class FSingleDelegatePropertyTranslator : public FDelegateBasePropertyTranslator
{
	public:
		FSingleDelegatePropertyTranslator(FPropertyTranslatorManager& InPropertyHandlers) : FDelegateBasePropertyTranslator(InPropertyHandlers, EPU_Any)
		{
		}

		//FPropertyTranslator interface implementation
		virtual bool CanHandleProperty(const FProperty* Property) const override;
		virtual void ExportParameterStaticConstruction(FCSScriptBuilder& Builder, const FString& NativeMethodName, const FProperty* Parameter) const override;
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
