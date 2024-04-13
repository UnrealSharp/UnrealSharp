#pragma once

#include "BlittableTypePropertyTranslator.h"

class FBlittableStructPropertyTranslator : public FBlittableTypePropertyTranslator
{
public:
	
	explicit FBlittableStructPropertyTranslator(FCSPropertyTranslatorManager& InPropertyHandlers);
	
	static bool IsStructBlittable(const FCSPropertyTranslatorManager& PropertyHandlers, const UScriptStruct& ScriptStruct);

	//FPropertyTranslator interface implementation
	virtual bool CanHandleProperty(const FProperty* Property) const override;
	virtual FString GetManagedType(const FProperty* Property) const override;
	virtual void AddReferences(const FProperty* Property, TSet<UField*>& References) const override;
protected:
	virtual bool CanExportDefaultParameter() const override { return false; }
	virtual void ExportCppDefaultParameterAsLocalVariable(FCSScriptBuilder& Builder, const FString& VariableName, const FString& CppDefaultValue, UFunction* Function, FProperty* ParamProperty) const override;
	//End of implementation
};
