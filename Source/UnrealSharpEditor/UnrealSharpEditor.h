#pragma once

#include "CoreMinimal.h"
#include "Modules/ModuleManager.h"
#include "Containers/Ticker.h"

class FUnrealSharpEditorModule : public IModuleInterface
{
public:

    // IModuleInterface interface begin
    virtual void StartupModule() override;
    virtual void ShutdownModule() override;
    // End
    
    void OnCSharpCodeModified(const TArray<struct FFileChangeData>& ChangedFiles);
    void Reload();

    FTickerDelegate TickDelegate;
    FTSTicker::FDelegateHandle TickDelegateHandle;

    bool Tick(float DeltaTime);

    FDelegateHandle ModuleLoadingPhaseCompleteDelegateHandle;
    void OnAllModuleLoadingPhasesComplete();

    bool bIsReloading = false;
};
