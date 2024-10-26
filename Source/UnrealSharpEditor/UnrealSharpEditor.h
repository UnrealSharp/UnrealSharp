#pragma once

#include "CoreMinimal.h"
#include "Modules/ModuleManager.h"
#include "Containers/Ticker.h"

class FCSScriptBuilder;

enum HotReloadStatus
{
    // Not Hot Reloading
    Inactive,
    // When the DirectoryWatcher picks up on new code changes that haven't been Hot Reloaded yet
    PendingReload,
    // Actively Hot Reloading
    Active
};

DECLARE_LOG_CATEGORY_EXTERN(LogUnrealSharpEditor, Log, All);

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

    static void OnCreateNewProject();
    static void OnCompileManagedCode();
    static void OnRegenerateSolution();
    static void OnOpenSolution();
    static void OnPackageProject();
    static void OnOpenSettings();
    static void OnOpenDocumentation();
    static void OnReportBug();
    static void OnExploreArchiveDirectory(FString ArchiveDirectory);

    static void PackageProject();

    TSharedRef<SWidget> GenerateUnrealSharpMenu();
    
    static void OpenNewProjectDialog(const FString& SuggestedProjectName = FString());

    static void SuggestProjectSetup();
    
    bool Tick(float DeltaTime);
    
    void RegisterCommands();
    void RegisterMenu();
    void RegisterGameplayTags();
    void RegisterAssetTypes();

    static void SaveRuntimeGlue(const FCSScriptBuilder& ScriptBuilder, const FString& FileName);

    static void OnAssetSearchRootAdded(const FString& RootPath);
    static void OnCompletedInitialScan();

    static bool IsRegisteredAssetType(const FAssetData& AssetData);
    static bool IsRegisteredAssetType(UClass* Class);
    
    static void OnAssetRemoved(const FAssetData& AssetData);
    static void OnAssetRenamed(const FAssetData& AssetData, const FString& OldObjectPath);
    static void OnInMemoryAssetCreated(UObject* Object);
    static void OnInMemoryAssetDeleted(UObject* Object);

    static void OnAssetManagerSettingsChanged(UObject* Object, FPropertyChangedEvent& PropertyChangedEvent);

    static void WaitUpdateAssetTypes();

    static void ProcessGameplayTags();
    
    static void ProcessAssetIds();
    static void ProcessAssetTypes();
    
    FSlateIcon GetMenuIcon() const;
    
    HotReloadStatus HotReloadStatus = Inactive;
    bool bHotReloadFailed = false;
    
    static FString QuotePath(const FString& Path);
    FTickerDelegate TickDelegate;
    FTSTicker::FDelegateHandle TickDelegateHandle;
    TSharedPtr<FUICommandList> UnrealSharpCommands;
};
