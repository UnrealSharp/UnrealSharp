#pragma once

#include "CoreMinimal.h"
#include "Compiler/FCSCompilerContext.h"
#include "Modules/ModuleManager.h"

class FUnrealSharpCompilerModule : public IModuleInterface
{
public:
    virtual void StartupModule() override;
    virtual void ShutdownModule() override;

private:
    FCSBlueprintCompiler CSCompiler;
};
