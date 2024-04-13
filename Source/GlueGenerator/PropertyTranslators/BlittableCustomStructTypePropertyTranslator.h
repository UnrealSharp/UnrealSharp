#pragma once
#include "BlittableTypePropertyTranslator.h"

class FBlittableCustomStructTypePropertyTranslator : public FBlittableTypePropertyTranslator
{
public:
	
	FBlittableCustomStructTypePropertyTranslator(FCSPropertyTranslatorManager& InPropertyHandlers, const FString& InUnrealStructName, const FString& CSharpStructName);

	//FPropertyTranslator interface implementation
	virtual bool CanHandleProperty(const FProperty* Property) const override;
protected:
	virtual bool CanExportDefaultParameter() const override { return false; }
	virtual void ExportCppDefaultParameterAsLocalVariable(FCSScriptBuilder& Builder, const FString& VariableName, const FString& CppDefaultValue, UFunction* Function, FProperty* ParamProperty) const override;
	//End of implementation
	
private:
	FName UnrealStructName;
};
