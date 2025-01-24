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
    static void OnNewClass(UClass* OldClass, UClass* NewClass);
    static void OnManagedAssemblyLoaded(const FString& AssemblyName);

    FCSBlueprintCompiler BlueprintCompiler;
};
