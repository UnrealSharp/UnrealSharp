#pragma once

#include "CoreMinimal.h"
#include "Modules/ModuleManager.h"
#include "Containers/Ticker.h"

enum HotReloadStatus
{
    // Not Hot Reloading
    Inactive,
    // When the DirectoryWatcher picks up on new code changes that haven't been Hot Reloaded yet
    PendingReload,
    // Actively Hot Reloading
    Active
};

class FUnrealSharpEditorModule : public IModuleInterface
{
public:
    static FUnrealSharpEditorModule& Get();
    
    // IModuleInterface interface begin
    virtual void StartupModule() override;
    virtual void ShutdownModule() override;
    // End
    
    void OnCSharpCodeModified(const TArray<struct FFileChangeData>& ChangedFiles);
    void StartHotReload();

    bool IsHotReloading() const { return HotReloadStatus == Active; }
    bool HasPendingHotReloadChanges() const { return HotReloadStatus == PendingReload; }
    bool HasHotReloadFailed() const { return bHotReloadFailed; }
    
    static void OpenSolution();

private:
    static FString SelectArchiveDirectory();

    static void RunGame(FString ExecutablePath);

    void OnUnrealSharpInitialized();

    static void OnCreateNewProject();
    static void OnCompileManagedCode();
    static void OnRegenerateSolution();
    static void OnOpenSolution();
    static void OnPackageProject();
    static void OnOpenSettings();
    static void OnOpenDocumentation();
    static void OnReportBug();

    static void PackageProject();

    TSharedRef<SWidget> GenerateUnrealSharpMenu();
    
    static void OpenNewProjectDialog(const FString& SuggestedProjectName = FString());

    static void SuggestProjectSetup();
    
    bool Tick(float DeltaTime);
    
    void RegisterCommands();
    void RegisterMenu();
    FSlateIcon GetMenuIcon() const;
    
    HotReloadStatus HotReloadStatus = Inactive;
    bool bHotReloadFailed = false;
    
    static FString QuotePath(const FString& Path);
    FTickerDelegate TickDelegate;
    FTSTicker::FDelegateHandle TickDelegateHandle;
    TSharedPtr<FUICommandList> UnrealSharpCommands;
};
