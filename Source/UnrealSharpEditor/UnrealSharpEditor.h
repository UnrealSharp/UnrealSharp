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
    void StartHotReload();

    bool IsReloading() const { return bIsReloading; }

private:

    void OnUnrealSharpInitialized();
    
    bool Tick(float DeltaTime);
    
    FTickerDelegate TickDelegate;
    FTSTicker::FDelegateHandle TickDelegateHandle;
    bool bIsReloading = false;

    void RegisterMenus();
};
