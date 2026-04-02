#pragma once
#include "KismetCompiler.h"
#include "Utilities/CSClassUtilities.h"

struct FCSManagedTypeDefinition;
struct FCSClassReflectionData;
struct FCSPropertyReflectionData;
class UCSClass;
class UCSBlueprint;

class FCSCompilerContext : public FKismetCompilerContext
{
public:

	FCSCompilerContext(UCSBlueprint* Blueprint, FCompilerResultsLog& InMessageLog, const FKismetCompilerOptions& InCompilerOptions);

	// FKismetCompilerContext interface
	virtual void FinishCompilingClass(UClass* Class) override;
	virtual void OnPostCDOCompiled(const UObject::FPostCDOCompiledContext& Context) override;
	virtual void CreateClassVariablesFromBlueprint() override;
	virtual void CleanAndSanitizeClass(UBlueprintGeneratedClass* ClassToClean, UObject*& OldCDO) override;
	virtual void SpawnNewClass(const FString& NewClassName) override;
	virtual void AddInterfacesFromBlueprint(UClass* Class) override;
	virtual void CopyTermDefaultsToDefaultObject(UObject* DefaultObject) override;
	// End of FKismetCompilerContext interface
	
protected:
	typedef FKismetCompilerContext Super;
private:
	
	void ValidateSimpleConstructionScript() const;
	void GenerateFunctions() const;
	
	UCSClass* GetMainClass() const;
	
	TSharedPtr<const FCSManagedTypeDefinition> GetClassInfo() const;
	TSharedPtr<const FCSClassReflectionData> GetReflectionData() const;
	
	void TryInitializeAsDeveloperSettings(const UClass* Class) const;
	void TryDeinitializeAsDeveloperSettings(UObject* Settings) const;

	static void TryFakeNativeClass(UClass* Class);
	
	void ApplyMetaData() const;

	void CreateDummyBlueprintVariables(const TArray<FCSPropertyReflectionData>& Properties) const;
};

