#pragma once

class FCSTypeWizardOption;

class FCSTypeWizardOptionRegistry
{
public:
	static FCSTypeWizardOptionRegistry& Get();

	void RegisterOption(const TSharedRef<FCSTypeWizardOption>& Option);

	template<typename WizardTypeOption, typename... ArgumentTypes>
	TSharedRef<WizardTypeOption> RegisterOption(ArgumentTypes&&... Args)
	{
		TSharedRef<WizardTypeOption> Option = MakeShared<WizardTypeOption>(Forward<ArgumentTypes>(Args)...);
		RegisterOption(StaticCastSharedRef<FCSTypeWizardOption>(Option));
		return Option;
	}

	const TArray<TSharedRef<FCSTypeWizardOption>>& GetOptions() const { return Options; }
	TSharedPtr<FCSTypeWizardOption> FindOption(FName OptionName) const;
	TSharedPtr<FCSTypeWizardOption> GetDefaultOption() const;

private:
	FCSTypeWizardOptionRegistry();

	TArray<TSharedRef<FCSTypeWizardOption>> Options;
};
