#pragma once

#include "CoreMinimal.h"
#include "CSBlueprintCompiler.h"
#include "Modules/ModuleManager.h"

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
    void OnNewInterface(UCSInterface* NewInterface);
    
    void OnManagedAssemblyLoaded(const FName& AssemblyName);
    void RecompileAndReinstanceBlueprints();

    void AddManagedReferences(FCSManagedReferencesCollection& Collection);

    FCSBlueprintCompiler BlueprintCompiler;
    
    TArray<UBlueprint*> ManagedClassesToCompile;
    TArray<UBlueprint*> ManagedComponentsToCompile;
};