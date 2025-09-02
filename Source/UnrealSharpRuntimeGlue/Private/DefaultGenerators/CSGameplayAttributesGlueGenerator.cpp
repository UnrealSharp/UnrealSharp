#include "DefaultGenerators/CSGameplayAttributesGlueGenerator.h"
#include "CSScriptBuilder.h"
#include "AttributeSet.h"
#include "Extensions/Subsystems/GameplayAttributeSubsystem.h"

void UCSGameplayAttributesGlueGenerator::Initialize()
{
UE_LOG(LogTemp, Log, TEXT("CSGameplayAttributesGlueGenerator: Initializing..."));

ProcessGameplayAttributes();
}

void UCSGameplayAttributesGlueGenerator::ProcessGameplayAttributes()
{
// Ensure the subsystem is initialized and has cached attributes
UGameplayAttributeSubsystem* AttributeSubsystem = UGameplayAttributeSubsystem::Get();
if (!AttributeSubsystem)
{
	UE_LOG(LogTemp, Error, TEXT("CSGameplayAttributesGlueGenerator: Failed to get GameplayAttributeSubsystem"));
	return;
}

TArray<UClass*> AttributeSetClasses;
FindAllAttributeSetClasses(AttributeSetClasses);

UE_LOG(LogTemp, Log, TEXT("CSGameplayAttributesGlueGenerator: Found %d AttributeSet classes"), AttributeSetClasses.Num());

FCSScriptBuilder ScriptBuilder(FCSScriptBuilder::IndentType::Tabs);
ScriptBuilder.AppendLine(TEXT("using UnrealSharp.GameplayAbilities;"));
ScriptBuilder.AppendLine(TEXT("using UnrealSharp.CoreUObject;"));
ScriptBuilder.AppendLine(TEXT("using UnrealSharp.UnrealSharpCore;"));
ScriptBuilder.AppendLine(TEXT("using UnrealSharp.Engine;"));
ScriptBuilder.AppendLine();
ScriptBuilder.AppendLine(TEXT("public static class GameplayAttributes"));
ScriptBuilder.OpenBrace();

if (AttributeSetClasses.Num() == 0)
{
	ScriptBuilder.AppendLine(TEXT("// No AttributeSet classes found"));
	UE_LOG(LogTemp, Log, TEXT("CSGameplayAttributesGlueGenerator: No AttributeSet classes found"));
}
else
{
	// Generate attributes for each AttributeSet class
	for (UClass* AttributeSetClass : AttributeSetClasses)
	{
		UE_LOG(LogTemp, Log, TEXT("CSGameplayAttributesGlueGenerator: Processing AttributeSet class: %s"), *AttributeSetClass->GetName());
		GenerateAttributesForClass(AttributeSetClass, ScriptBuilder, AttributeSubsystem);
	}
}

ScriptBuilder.CloseBrace(); // Close GameplayAttributes class

UE_LOG(LogTemp, Log, TEXT("CSGameplayAttributesGlueGenerator: Saving GameplayAttributes.cs"));
SaveRuntimeGlue(ScriptBuilder, TEXT("GameplayAttributes"));
}

void UCSGameplayAttributesGlueGenerator::FindAllAttributeSetClasses(TArray<UClass*>& OutAttributeSetClasses)
{
OutAttributeSetClasses.Empty();

UE_LOG(LogTemp, Log, TEXT("CSGameplayAttributesGlueGenerator: Scanning for AttributeSet classes..."));

// Iterate through all loaded classes
for (TObjectIterator<UClass> ClassIterator; ClassIterator; ++ClassIterator)
{
	UClass* Class = *ClassIterator;

	// Check if it's a child of UAttributeSet and not abstract
	// Also filter out Blueprint-generated classes to be consistent with engine behavior
	if (Class && Class->IsChildOf(UAttributeSet::StaticClass()) &&
		!Class->HasAnyClassFlags(CLASS_Abstract) &&
		!Class->ClassGeneratedBy &&
		Class != UAttributeSet::StaticClass())
	{
		UE_LOG(LogTemp, Log, TEXT("CSGameplayAttributesGlueGenerator: Found AttributeSet class: %s"), *Class->GetName());
		OutAttributeSetClasses.Add(Class);
	}
}

// Sort by class name for consistent output
OutAttributeSetClasses.Sort([](const UClass& A, const UClass& B)
{
	return A.GetName() < B.GetName();
});

UE_LOG(LogTemp, Log, TEXT("CSGameplayAttributesGlueGenerator: Total AttributeSet classes found: %d"), OutAttributeSetClasses.Num());
}

void UCSGameplayAttributesGlueGenerator::GenerateAttributesForClass(UClass* AttributeSetClass, FCSScriptBuilder& ScriptBuilder, UGameplayAttributeSubsystem* AttributeSubsystem)
{
if (!AttributeSetClass || !AttributeSubsystem)
{
	return;
}

// Use cached attribute names for better performance
TArray<FString> AttributeNames;
AttributeSubsystem->GetCachedAttributeNamesForClass(AttributeSetClass->GetName(), AttributeNames);

if (AttributeNames.Num() == 0)
{
	return;
}

FString ClassName = ClassNameToValidName(AttributeSetClass->GetName());

ScriptBuilder.AppendLine(FString::Printf(TEXT("// Attributes from %s"), *AttributeSetClass->GetName()));

for (const FString& PropertyName : AttributeNames)
{
	FString VariableName = PropertyNameToVariableName(PropertyName);
	FString FullVariableName = FString::Printf(TEXT("%s_%s"), *ClassName, *VariableName);

	// Generate private backing field for lazy initialization
	ScriptBuilder.AppendLine(FString::Printf(TEXT("private static FGameplayAttribute? _%s;"), *FullVariableName));

	// Generate public property with lazy initialization
	ScriptBuilder.AppendLine(FString::Printf(TEXT("/// <summary>")));
	ScriptBuilder.AppendLine(FString::Printf(TEXT("/// %s attribute from %s"), *PropertyName, *AttributeSetClass->GetName()));
	ScriptBuilder.AppendLine(FString::Printf(TEXT("/// </summary>")));
	ScriptBuilder.AppendLine(FString::Printf(TEXT("public static FGameplayAttribute %s"), *FullVariableName));
	ScriptBuilder.OpenBrace();
	ScriptBuilder.AppendLine(TEXT("get"));
	ScriptBuilder.OpenBrace();
	ScriptBuilder.AppendLine(FString::Printf(TEXT("if (_%s == null)"), *FullVariableName));
	ScriptBuilder.OpenBrace();
	// Resolve FGameplayAttribute by native class/property name to avoid requiring managed AttributeSet types
	ScriptBuilder.AppendLine(FString::Printf(TEXT("_%s = UGameplayAttributeSubsystem.FindGameplayAttributeByName(\"%s\", \"%s\");"),
		*FullVariableName, *AttributeSetClass->GetName(), *PropertyName));
	ScriptBuilder.CloseBrace();
	ScriptBuilder.AppendLine(FString::Printf(TEXT("return _%s ?? new FGameplayAttribute();"), *FullVariableName));
	ScriptBuilder.CloseBrace();
	ScriptBuilder.CloseBrace();
	ScriptBuilder.AppendLine();
}

ScriptBuilder.AppendLine();
}

FString UCSGameplayAttributesGlueGenerator::PropertyNameToVariableName(const FString& PropertyName)
{
	// Convert property name to a valid C# variable name
	FString VariableName = PropertyName;

	// Remove any invalid characters and replace with underscores
	VariableName = VariableName.Replace(TEXT(" "), TEXT("_"));
	VariableName = VariableName.Replace(TEXT("-"), TEXT("_"));
	VariableName = VariableName.Replace(TEXT("."), TEXT("_"));

	// Ensure it starts with a letter or underscore
	if (VariableName.Len() > 0 && !FChar::IsAlpha(VariableName[0]) && VariableName[0] != '_')
	{
		VariableName = TEXT("_") + VariableName;
	}

	return VariableName;
}

FString UCSGameplayAttributesGlueGenerator::ClassNameToValidName(const FString& ClassName)
{
	// Convert class name to a valid C# class name
	FString ValidName = ClassName;

	// Remove the 'U' prefix if present
	if (ValidName.StartsWith(TEXT("U")) && ValidName.Len() > 1)
	{
		ValidName = ValidName.Mid(1);
	}

	// Remove any invalid characters
	ValidName = ValidName.Replace(TEXT(" "), TEXT(""));
	ValidName = ValidName.Replace(TEXT("-"), TEXT(""));
	ValidName = ValidName.Replace(TEXT("."), TEXT(""));

	return ValidName;
}
