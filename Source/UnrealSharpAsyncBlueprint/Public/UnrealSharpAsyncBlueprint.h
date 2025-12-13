#pragma once

#include "CoreMinimal.h"
#include "Modules/ModuleManager.h"

class FUnrealSharpAsyncBlueprintModule : public IModuleInterface
{
public:

    // IModuleInterface interface begin
    virtual void StartupModule() override;
    virtual void ShutdownModule() override;
    // End
};
