#pragma once

#include "CoreMinimal.h"
#include "Modules/ModuleManager.h"

class FUnrealSharpEditorModule : public IModuleInterface
{
public:

    // IModuleInterface interface begin
    virtual void StartupModule() override;
    virtual void ShutdownModule() override;
    // End
    
    void OnCSharpCodeModified(const TArray<struct FFileChangeData>& ChangedFiles);
    void Reload();

    bool bIsReloading = false;
};
