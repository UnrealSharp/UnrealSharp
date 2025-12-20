#pragma once

#include "CoreMinimal.h"
#include "CSBlueprintCompiler.h"
#include "Modules/ModuleManager.h"

class UCSManagedAssembly;
struct FCSManagedTypeDefinition;
class UCSInterface;
struct FCSManagedReferencesCollection;
class UCSEnum;
class UCSScriptStruct;
class FCSBlueprintCompiler;

DECLARE_LOG_CATEGORY_EXTERN(LogUnrealSharpCompiler, Log, All);

class FUnrealSharpCompilerModule : public IModuleInterface
{
public:
    virtual void StartupModule() override;
    virtual void ShutdownModule() override;
private:
    
    void OnNewClass(UCSClass* NewClass);
    void OnNewStruct(UCSScriptStruct* NewStruct);
    void OnNewEnum(UCSEnum* NewEnum);

    void OnReflectionDataChanged(TSharedPtr<FCSManagedTypeDefinition> ManagedTypeDefinition);
    
    void OnManagedAssemblyLoaded(const UCSManagedAssembly* Assembly);
    void RecompileAndReinstanceBlueprints();

    void AddManagedReferences(FCSManagedReferencesCollection& Collection);

    static void InvalidateReferences(UBlueprint* Blueprint);
    
    FCSBlueprintCompiler BlueprintCompiler;
    
    TArray<UCSBlueprint*> ManagedClassesToCompile;
    TArray<UCSBlueprint*> ManagedComponentsToCompile;
};