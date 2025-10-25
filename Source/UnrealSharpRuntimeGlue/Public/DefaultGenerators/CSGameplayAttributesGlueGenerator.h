#pragma once

#include "CoreMinimal.h"
#include "CSGlueGenerator.h"
#include "Extensions/Subsystems/CSGameplayAttributeSubsystem.h"
#include "CSGameplayAttributesGlueGenerator.generated.h"

UCLASS(DisplayName="Gameplay Attributes Glue Generator", NotBlueprintable, NotBlueprintType)
class UNREALSHARPRUNTIMEGLUE_API UCSGameplayAttributesGlueGenerator : public UCSGlueGenerator
{
	GENERATED_BODY()

private:
	// UCSGlueGenerator interface
	virtual void Initialize() override;
	virtual void ForceRefresh() override { ProcessGameplayAttributes(); }
	// End of UCSGlueGenerator interface

	/**
	 * Process all AttributeSet classes and generate C# code for their attributes
	 */
	void ProcessGameplayAttributes();

	/**
	 * Scan for all UAttributeSet-derived classes
	 * @param OutAttributeSetClasses Array to fill with found AttributeSet classes
	 */
	void FindAllAttributeSetClasses(TArray<UClass*>& OutAttributeSetClasses);

	/**
	 * Generate C# code for a specific AttributeSet class
	 * @param AttributeSetClass The AttributeSet class to process
	 * @param ScriptBuilder The script builder to append code to
	 * @param AttributeSubsystem The gameplay attribute subsystem for cached lookups
	 */
	void GenerateAttributesForClass(UClass* AttributeSetClass, class FCSScriptBuilder& ScriptBuilder, UCSGameplayAttributeSubsystem* AttributeSubsystem);

	/**
	 * Convert a property name to a valid C# variable name
	 * @param PropertyName The original property name
	 * @return A valid C# variable name
	 */
	FString PropertyNameToVariableName(const FString& PropertyName);

	/**
	 * Convert a class name to a valid C# class name
	 * @param ClassName The original class name
	 * @return A valid C# class name
	 */
	FString ClassNameToValidName(const FString& ClassName);
};
