#pragma once
#include "KismetCompiler.h"

struct FCSPropertyMetaData;
struct FCSharpClassInfo;
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
	// End of FKismetCompilerContext interface
protected:
	typedef FKismetCompilerContext Super;
private:
	void TryValidateSimpleConstructionScript(const TSharedPtr<const FCSharpClassInfo>& ClassInfo) const;
	void GenerateFunctions() const;
	UCSClass* GetMainClass() const;
	TSharedPtr<const FCSharpClassInfo> GetClassInfo() const;

	bool IsDeveloperSettings() const;
	void TryInitializeAsDeveloperSettings(const UClass* Class) const;
	void TryDeinitializeAsDeveloperSettings(UObject* Settings) const;
	void ApplyMetaData();

	void CreateDummyBlueprintVariables(const TArray<FCSPropertyMetaData>& Properties) const;
};

