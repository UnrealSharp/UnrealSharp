#pragma once
#include "BlittableTypePropertyTranslator.h"

class FNamePropertyTranslator : public FBlittableTypePropertyTranslator
{
public:
	
	explicit FNamePropertyTranslator(FCSPropertyTranslatorManager& InPropertyHandlers);

protected:

	//FPropertyTranslator interface implementation
	virtual FString GetNullReturnCSharpValue(const FProperty* ReturnProperty) const override;
	virtual bool CanExportDefaultParameter() const override { return false; }
	virtual void ExportCppDefaultParameterAsLocalVariable(FCSScriptBuilder& Builder, const FString& VariableName, const FString& CppDefaultValue, UFunction* Function, FProperty* ParamProperty) const override;
	//End of implementation

};
