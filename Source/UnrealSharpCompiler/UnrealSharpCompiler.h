#pragma once

#include "CoreMinimal.h"
#include "CSBlueprintCompiler.h"
#include "Modules/ModuleManager.h"

class FCSBlueprintCompiler;

class FUnrealSharpCompilerModule : public IModuleInterface
{
public:
    virtual void StartupModule() override;
    virtual void ShutdownModule() override;
private:
    
    void OnNewClass(UClass* NewClass);
    void OnManagedAssemblyLoaded(const FName& AssemblyName);
    void RecompileAndReinstanceBlueprints();

    FCSBlueprintCompiler BlueprintCompiler;
    
    TArray<UBlueprint*> ManagedClassesToCompile;
    TArray<UBlueprint*> ManagedComponentsToCompile;
};
