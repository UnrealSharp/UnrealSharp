#include "Slate/TypeWizardOption/CSTypeWizardOptionRegistry.h"

#include "Slate/TypeWizardOption/CSClassTypeWizardOption.h"

#define LOCTEXT_NAMESPACE "CSTypeOptionRegistry"

FCSTypeWizardOptionRegistry& FCSTypeWizardOptionRegistry::Get()
{
	static FCSTypeWizardOptionRegistry Registry;
	return Registry;
}

FCSTypeWizardOptionRegistry::FCSTypeWizardOptionRegistry()
{
	RegisterOption<FCSClassTypeWizardOption>();

	{
		FCSAttributedTypeWizardOption::FCSAttributedOptionDescriptor Descriptor;
		Descriptor.OptionName = TEXT("Struct");
		Descriptor.DisplayName = LOCTEXT("StructDisplayName", "Struct");
		Descriptor.NameHint = LOCTEXT("StructNameHint", "MyStruct");
		Descriptor.Prefix = TEXT("F");
		Descriptor.AttributeName = TEXT("UStruct");
		Descriptor.Keyword = TEXT("struct");
		Descriptor.ConflictScope = ECSTypeConflictScope::ScriptStruct;
		Descriptor.SortPriority = 10;

		RegisterOption<FCSAttributedTypeWizardOption>(MoveTemp(Descriptor));
	}

	{
		FCSAttributedTypeWizardOption::FCSAttributedOptionDescriptor Descriptor;
		Descriptor.OptionName = TEXT("Enum");
		Descriptor.DisplayName = LOCTEXT("EnumDisplayName", "Enum");
		Descriptor.NameHint = LOCTEXT("EnumNameHint", "MyEnum");
		Descriptor.Prefix = TEXT("E");
		Descriptor.AttributeName = TEXT("UEnum");
		Descriptor.Keyword = TEXT("enum");
		Descriptor.Modifiers = TEXT("public");
		Descriptor.DeclarationSuffix = TEXT(" : byte");
		Descriptor.ConflictScope = ECSTypeConflictScope::Enum;
		Descriptor.SortPriority = 20;

		RegisterOption<FCSAttributedTypeWizardOption>(MoveTemp(Descriptor));
	}

	{
		FCSAttributedTypeWizardOption::FCSAttributedOptionDescriptor Descriptor;
		Descriptor.OptionName = TEXT("Interface");
		Descriptor.DisplayName = LOCTEXT("InterfaceDisplayName", "Interface");
		Descriptor.NameHint = LOCTEXT("InterfaceNameHint", "MyInterface");
		Descriptor.Prefix = TEXT("I");
		Descriptor.AttributeName = TEXT("UInterface");
		Descriptor.Keyword = TEXT("interface");
		Descriptor.ConflictScope = ECSTypeConflictScope::Class;
		Descriptor.SortPriority = 30;

		RegisterOption<FCSAttributedTypeWizardOption>(MoveTemp(Descriptor));
	}
}

void FCSTypeWizardOptionRegistry::RegisterOption(const TSharedRef<FCSTypeWizardOption>& Option)
{
	Options.Add(Option);

	Options.StableSort([](const TSharedRef<FCSTypeWizardOption>& Lhs, const TSharedRef<FCSTypeWizardOption>& Rhs)
	{
		if (Lhs->GetSortPriority() != Rhs->GetSortPriority())
		{
			return Lhs->GetSortPriority() < Rhs->GetSortPriority();
		}

		return Lhs->GetDisplayName().CompareTo(Rhs->GetDisplayName()) < 0;
	});
}

TSharedPtr<FCSTypeWizardOption> FCSTypeWizardOptionRegistry::FindOption(FName OptionName) const
{
	const TSharedRef<FCSTypeWizardOption>* FoundOption = Options.FindByPredicate([OptionName](const TSharedRef<FCSTypeWizardOption>& Option)
	{
		return Option->GetOptionName() == OptionName;
	});
 
	return FoundOption != nullptr ? *FoundOption : TSharedPtr<FCSTypeWizardOption>();
}

TSharedPtr<FCSTypeWizardOption> FCSTypeWizardOptionRegistry::GetDefaultOption() const
{
	return Options.IsEmpty() ? TSharedPtr<FCSTypeWizardOption>() : Options[0];
}

#undef LOCTEXT_NAMESPACE