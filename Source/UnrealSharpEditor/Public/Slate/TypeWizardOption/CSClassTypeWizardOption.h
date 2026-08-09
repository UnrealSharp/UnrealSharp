#pragma once

#include "CSAttributedTypeWizardOption.h"
#include "CSTypeWizardOption.h"

class FCSClassTypeWizardOption : public FCSAttributedTypeWizardOption
{
public:
	FCSClassTypeWizardOption();

	// FCSTypeWizardOption interface implementation
	virtual FString GetPrefix(const UClass* ParentClass) const override;
	virtual bool RequiresParentClass() const override { return true; }
	virtual void CollectUsings(const FCSNewTypeParams& Params, TArray<FString>& OutUsings) const override;
	virtual void AppendDeclaration(const FCSNewTypeParams& Params, FCSScriptBuilder& ScriptBuilder) const override;
	virtual void AppendBody(const FCSNewTypeParams& Params, FCSScriptBuilder& ScriptBuilder) const override;
	virtual FText GetDeclarationPreview(const FCSNewTypeParams& Params) const override;
	// End
};
