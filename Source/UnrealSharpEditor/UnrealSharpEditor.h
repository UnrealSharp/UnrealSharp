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

DECLARE_MULTICAST_DELEGATE(FOnRefreshRuntimeGlue);

class FUnrealSharpEditorModule : public IModuleInterface
{
public:
    static FUnrealSharpEditorModule& Get();
    
    // IModuleInterface interface begin
    virtual void StartupModule() override;
    virtual void ShutdownModule() override;
    // End
    
    void OnCSharpCodeModified(const TArray<struct FFileChangeData>& ChangedFiles);
    void StartHotReload(bool bRebuild = true);

    bool IsHotReloading() const { return HotReloadStatus == Active; }
    bool HasPendingHotReloadChanges() const { return HotReloadStatus == PendingReload; }
    bool HasHotReloadFailed() const { return bHotReloadFailed; }

    FOnRefreshRuntimeGlue& OnRefreshRuntimeGlueEvent() { return OnRefreshRuntimeGlueDelegate; }
    
    static void SaveRuntimeGlue(const FCSScriptBuilder& ScriptBuilder, const FString& FileName, const FString& Suffix = FString(TEXT(".cs")));
    static void OpenSolution();

    static bool FillTemplateFile(const FString& TemplateName, TMap<FString, FString>& Replacements, const FString& Path);

private:
    static FString SelectArchiveDirectory();

    static void RunGame(FString ExecutablePath);

    static void OnCreateNewProject();
    static void OnCompileManagedCode();
    static void OnReloadManagedCode();
    static void OnRegenerateSolution();
    static void OnOpenSolution();
    static void OnPackageProject();
    static void OnOpenSettings();
    static void OnOpenDocumentation();
    static void OnReportBug();
    
    void OnRefreshRuntimeGlue() const;
    
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
    void RegisterCollisionProfile();

    static void OnAssetSearchRootAdded(const FString& RootPath);
    static void OnCompletedInitialScan();

    static bool IsRegisteredAssetType(const FAssetData& AssetData);
    static bool IsRegisteredAssetType(UClass* Class);
    
    static void OnAssetRemoved(const FAssetData& AssetData);
    static void OnAssetRenamed(const FAssetData& AssetData, const FString& OldObjectPath);
    static void OnInMemoryAssetCreated(UObject* Object);
    static void OnInMemoryAssetDeleted(UObject* Object);

    static void OnCollisionProfileLoaded(UCollisionProfile* Profile);

    static void OnAssetManagerSettingsChanged(UObject* Object, FPropertyChangedEvent& PropertyChangedEvent);

    void OnPIEEnded(bool IsSimulating);

    static void WaitUpdateAssetTypes();

    static void ProcessGameplayTags();
    static void ProcessAssetIds();
    static void ProcessAssetTypes();
    static void ProcessTraceTypeQuery();
    
    FSlateIcon GetMenuIcon() const;
    
    HotReloadStatus HotReloadStatus = Inactive;
    bool bHotReloadFailed = false;
    bool bHasQueuedHotReload = false;

    FOnRefreshRuntimeGlue OnRefreshRuntimeGlueDelegate;
    
    static FString QuotePath(const FString& Path);
    FTickerDelegate TickDelegate;
    FTSTicker::FDelegateHandle TickDelegateHandle;
    TSharedPtr<FUICommandList> UnrealSharpCommands;
};