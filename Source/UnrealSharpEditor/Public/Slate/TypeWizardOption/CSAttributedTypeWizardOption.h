#pragma once
#include "CSTypeWizardOption.h"

class FCSAttributedTypeWizardOption : public FCSTypeWizardOption
{
public:
	struct FCSAttributedOptionDescriptor
	{
		FName OptionName;
		FText DisplayName;
		FText NameHint;
		FString Prefix;
		FString AttributeName;
		FString Keyword;
		FString Modifiers = TEXT("public partial");
		FString DeclarationSuffix;
		TArray<FString> AdditionalUsings;
		ECSTypeConflictScope ConflictScope = ECSTypeConflictScope::Class;
		int32 SortPriority = 100;
	};

	FCSAttributedTypeWizardOption(FCSAttributedOptionDescriptor InDescriptor) : Descriptor(MoveTemp(InDescriptor)) {}

	// FCSTypeWizardOption interface implementation
	virtual FName GetOptionName() const override { return Descriptor.OptionName; }
	virtual FText GetDisplayName() const override { return Descriptor.DisplayName; }
	virtual FText GetNameHint() const override { return Descriptor.NameHint; }
	virtual int32 GetSortPriority() const override { return Descriptor.SortPriority; }
	virtual FString GetPrefix(const UClass* ParentClass) const override { return Descriptor.Prefix; }
	virtual ECSTypeConflictScope GetConflictScope() const override { return Descriptor.ConflictScope; }
	virtual void CollectUsings(const FCSNewTypeParams& Params, TArray<FString>& OutUsings) const override;
	virtual void AppendDeclaration(const FCSNewTypeParams& Params, FCSScriptBuilder& ScriptBuilder) const override;
	virtual FText GetDeclarationPreview(const FCSNewTypeParams& Params) const override;
	// End

	const FCSAttributedOptionDescriptor& GetOptionDescriptor() const { return Descriptor; }

protected:
	FString BuildDeclaration(const FCSNewTypeParams& Params) const;
	FCSAttributedOptionDescriptor Descriptor;
};
