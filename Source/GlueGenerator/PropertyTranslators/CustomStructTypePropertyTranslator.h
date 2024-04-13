#pragma once
#include "PrimitiveTypePropertyTranslator.h"

class FCustomStructTypePropertyTranslator : public FSimpleTypePropertyTranslator
{
public:
	FCustomStructTypePropertyTranslator(FCSPropertyTranslatorManager& InPropertyHandlers, const FString& InUnrealStructName, const FString& InCSharpStructName);

	//FPropertyTranslator interface implementation
	virtual bool CanHandleProperty(const FProperty* Property) const override;
	virtual void AddReferences(const FProperty* Property, TSet<UField*>& References) const override;
protected:
	virtual FString GetMarshaller(const FProperty *Property) const override;
	virtual bool CanExportDefaultParameter() const override { return false; }
	virtual void ExportCppDefaultParameterAsLocalVariable(FCSScriptBuilder& Builder, const FString& VariableName, const FString& CppDefaultValue, UFunction* Function, FProperty* ParamProperty) const override;
	//End of implementation
	
private:
	
	FName UnrealStructName;
};
