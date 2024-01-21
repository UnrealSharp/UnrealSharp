#pragma once

#include "CoreMinimal.h"
#include "Modules/ModuleManager.h"

class FCSGenerator;

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
