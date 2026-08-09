#include "Slate/TypeWizardOption/CSAttributedTypeWizardOption.h"

void FCSAttributedTypeWizardOption::CollectUsings(const FCSNewTypeParams& Params, TArray<FString>& OutUsings) const
{
	if (!Descriptor.AttributeName.IsEmpty())
	{
		OutUsings.AddUnique(TEXT("UnrealSharp.Attributes"));
	}

	for (const FString& AdditionalUsing : Descriptor.AdditionalUsings)
	{
		OutUsings.AddUnique(AdditionalUsing);
	}
}

FString FCSAttributedTypeWizardOption::BuildDeclaration(const FCSNewTypeParams& Params) const
{
	const FString TypeName = GetManagedTypeName(Params);
	const FString Prefix = !Descriptor.Modifiers.IsEmpty() ? Descriptor.Modifiers + TEXT(" ") : FString();
	return FString::Printf(TEXT("%s%s %s%s"), *Prefix, *Descriptor.Keyword, *TypeName, *Descriptor.DeclarationSuffix);
}

void FCSAttributedTypeWizardOption::AppendDeclaration(const FCSNewTypeParams& Params, FCSScriptBuilder& ScriptBuilder) const
{
	if (!Descriptor.AttributeName.IsEmpty())
	{
		ScriptBuilder.AppendLine(FString::Printf(TEXT("[%s]"), *Descriptor.AttributeName));
	}

	ScriptBuilder.AppendLine(BuildDeclaration(Params));
}

FText FCSAttributedTypeWizardOption::GetDeclarationPreview(const FCSNewTypeParams& Params) const
{
	return FText::FromString(BuildDeclaration(Params));
}