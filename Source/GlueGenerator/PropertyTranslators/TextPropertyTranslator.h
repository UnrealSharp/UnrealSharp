#pragma once

#include "BlittableTypePropertyTranslator.h"

class FTextPropertyTranslator : public FBlittableTypePropertyTranslator
{
public:
	
	explicit FTextPropertyTranslator(FCSPropertyTranslatorManager& InPropertyHandlers);

	//FPropertyTranslator interface implementation
	virtual FString GetNullReturnCSharpValue(const FProperty* ReturnProperty) const override;
	virtual bool CanExportDefaultParameter() const override { return false; }
	virtual void ExportCppDefaultParameterAsLocalVariable(FCSScriptBuilder& Builder, const FString& VariableName, const FString& CppDefaultValue, UFunction* Function, FProperty* ParamProperty) const override;
	virtual FString GetMarshaller(const FProperty* Property) const override;
	//End of implementation


};

