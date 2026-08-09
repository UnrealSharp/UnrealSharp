#include "Slate/TypeWizardOption/CSClassTypeWizardOption.h"

#include "CSNamespace.h"
#include "Slate/TypeWizardOption/CSTypeGenerator.h"

#define LOCTEXT_NAMESPACE "CSClassTypeOption"

FCSClassTypeWizardOption::FCSClassTypeWizardOption() : FCSAttributedTypeWizardOption([]
{
	FCSAttributedOptionDescriptor Descriptor;
	Descriptor.OptionName = TEXT("Class");
	Descriptor.DisplayName = LOCTEXT("ClassDisplayName", "Class");
	Descriptor.NameHint = LOCTEXT("ClassNameHint", "MyClass");
	Descriptor.AttributeName = TEXT("UClass");
	Descriptor.Keyword = TEXT("class");
	Descriptor.ConflictScope = ECSTypeConflictScope::Class;
	Descriptor.SortPriority = 0;
	return Descriptor;
}())
{
}

FString FCSClassTypeWizardOption::GetPrefix(const UClass* ParentClass) const
{
	if (!IsValid(ParentClass))
	{
		return FString();
	}

	if (ParentClass->HasAnyClassFlags(CLASS_Interface))
	{
		return TEXT("I");
	}

	return ParentClass->IsChildOf(AActor::StaticClass()) ? TEXT("A") : TEXT("U");
}

void FCSClassTypeWizardOption::CollectUsings(const FCSNewTypeParams& Params, TArray<FString>& OutUsings) const
{
	if (!IsValid(Params.ParentClass))
	{
		return;
	}

	FCSAttributedTypeWizardOption::CollectUsings(Params, OutUsings);

	const FCSNamespace ParentNamespace = FCSTypeGenerator::GetManagedNamespace(Params.ParentClass);
	if (ParentNamespace.IsValid())
	{
		OutUsings.AddUnique(ParentNamespace.GetName());
	}
}

void FCSClassTypeWizardOption::AppendDeclaration(const FCSNewTypeParams& Params, FCSScriptBuilder& ScriptBuilder) const
{
	const FString TypeName = GetManagedTypeName(Params);

	if (!IsValid(Params.ParentClass))
	{
		ScriptBuilder.AppendLine(FString::Printf(TEXT("public class %s"), *TypeName));
		return;
	}

	ScriptBuilder.AppendLine(FString::Printf(TEXT("[%s]"), *Descriptor.AttributeName));
	ScriptBuilder.AppendLine(FString::Printf(TEXT("%s %s %s : %s"), *Descriptor.Modifiers, 
	                                         *Descriptor.Keyword, 
	                                         *TypeName, 
	                                         *FCSTypeGenerator::GetParentTypeName(Params.ParentClass)));
}

void FCSClassTypeWizardOption::AppendBody(const FCSNewTypeParams& Params, FCSScriptBuilder& ScriptBuilder) const
{
	const UClass* ParentClass = Params.ParentClass;
	if (!IsValid(ParentClass) || (!ParentClass->IsChildOf(AActor::StaticClass()) && !ParentClass->IsChildOf(UActorComponent::StaticClass())))
	{
		return;
	}

	ScriptBuilder.AppendLine(TEXT("public override void BeginPlay()"));
	ScriptBuilder.OpenBrace();
	ScriptBuilder.AppendLine(TEXT("base.BeginPlay();"));
	ScriptBuilder.CloseBrace();
}

FText FCSClassTypeWizardOption::GetDeclarationPreview(const FCSNewTypeParams& Params) const
{
	const FString TypeName = GetManagedTypeName(Params);

	if (!IsValid(Params.ParentClass))
	{
		return FText::FromString(FString::Printf(TEXT("public class %s"), *TypeName));
	}

	return FText::FromString(FString::Printf(TEXT("public partial class %s : %s"), *TypeName, *FCSTypeGenerator::GetParentTypeName(Params.ParentClass)));
}

#undef LOCTEXT_NAMESPACE
