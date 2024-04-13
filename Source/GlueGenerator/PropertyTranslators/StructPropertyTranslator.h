#pragma once

#include "PrimitiveTypePropertyTranslator.h"

class FStructPropertyTranslator : public FSimpleTypePropertyTranslator
{
public:
	
	explicit FStructPropertyTranslator(FCSPropertyTranslatorManager& InPropertyHandlers);

	//FPropertyTranslator interface implementation
	virtual FString GetManagedType(const FProperty* Property) const override;
	virtual void AddReferences(const FProperty* Property, TSet<UField*>& References) const override;
protected:
	virtual FString GetMarshaller(const FProperty *Property) const;
	virtual bool CanExportDefaultParameter() const override { return false; }
	virtual void ExportCppDefaultParameterAsLocalVariable(FCSScriptBuilder& Builder, const FString& VariableName, const FString& CppDefaultValue, UFunction* Function, FProperty* ParamProperty) const override;
	//End of implementation

};
