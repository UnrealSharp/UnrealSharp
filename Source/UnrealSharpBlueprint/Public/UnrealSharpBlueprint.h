#pragma once

#include "CoreMinimal.h"
#include "Modules/ModuleManager.h"

class FUnrealSharpBlueprintModule : public IModuleInterface
{
public:

    // IModuleInterface interface begin
    virtual void StartupModule() override;
    virtual void ShutdownModule() override;
    // End
};
