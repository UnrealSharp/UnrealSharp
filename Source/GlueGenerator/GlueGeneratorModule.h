#pragma once

#include "CoreMinimal.h"
#include "Modules/ModuleManager.h"

class FCSGenerator;

#define GLUE_GENERATOR_VERSION 2
#define GLUE_GENERATOR_CONFIG TEXT("GlueGeneratorSettings")
#define GLUE_GENERATOR_VERSION_KEY TEXT("GlueGeneratorVersion")

DECLARE_LOG_CATEGORY_EXTERN(LogGlueGenerator, Log, All);

class FGlueGeneratorModule : public IModuleInterface
{
public:
    
    virtual void StartupModule() override;
    virtual void ShutdownModule() override;
    

private:
    
    void StartGeneratingGlue();
    
    TUniquePtr<FCSGenerator> CodeGenerator;
};
