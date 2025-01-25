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
    void OnNewClass(UClass* OldClass, UClass* NewClass);
    void OnManagedAssemblyLoaded(const FString& AssemblyName);

    void RecompileAndReinstanceBlueprints();

    FCSBlueprintCompiler BlueprintCompiler;
    
    TArray<UBlueprint*> OtherManagedClasses;
    TArray<UBlueprint*> ManagedComponentsToCompile;
};
