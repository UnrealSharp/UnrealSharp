#pragma once

#include "CoreMinimal.h"
#include "Modules/ModuleManager.h"
#include "Containers/Ticker.h"

#ifdef __clang__
#pragma clang diagnostic ignored "-Wignored-attributes"
#endif

enum ECSLoggerVerbosity : uint8;
class UCSInterface;
class UCSEnum;
class UCSClass;
class UCSScriptStruct;
class UCSManager;
struct FCSAssembly;
class IAssetTools;
class FCSScriptBuilder;

enum HotReloadStatus
{
    // Not Hot Reloading
    Inactive,
    // When the DirectoryWatcher picks up on new code changes that haven't been Hot Reloaded yet
    PendingReload,
    // Actively Hot Reloading
    Active,
    // Failed to unload an assembly during Hot Reload
    FailedToUnload
};

struct FCSManagedUnrealSharpEditorCallbacks
{
    FCSManagedUnrealSharpEditorCallbacks() : Build(nullptr), ForceManagedGC(nullptr)
    {
    }

    using FBuildProject = bool(__stdcall*)(const TCHAR*, const TCHAR*, const TCHAR*, void*, ECSLoggerVerbosity, void*, bool);
    using FForceManagedGC = void(__stdcall*)();
    using FOpenSolution = bool(__stdcall*)(const TCHAR*, void*);

    FBuildProject Build;
    FForceManagedGC ForceManagedGC;
    FOpenSolution OpenSolution;
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
    void StartHotReload(bool bRebuild = true, bool bPromptPlayerWithNewProject = true);

    void InitializeUnrealSharpEditorCallbacks(FCSManagedUnrealSharpEditorCallbacks Callbacks);

    bool IsHotReloading() const { return HotReloadStatus == Active; }
    bool HasPendingHotReloadChanges() const { return HotReloadStatus == PendingReload; }
    bool HasHotReloadFailed() const { return bHotReloadFailed; }

    FOnRefreshRuntimeGlue& OnRefreshRuntimeGlueEvent() { return OnRefreshRuntimeGlueDelegate; }
    
    void SaveRuntimeGlue(const FCSScriptBuilder& ScriptBuilder, const FString& FileName, const FString& Suffix = FString(TEXT(".cs")));
    void OpenSolution();

    static bool FillTemplateFile(const FString& TemplateName, TMap<FString, FString>& Replacements, const FString& Path);

    static void RepairComponents();

private:
    static FString SelectArchiveDirectory();

    static void RunGame(FString ExecutablePath);

    static void CopyProperties(UActorComponent* Source, UActorComponent* Target);

    static void OnCreateNewProject();
    static void OnCompileManagedCode();
    static void OnReloadManagedCode();
    void OnRegenerateSolution();
    void OnOpenSolution();
    static void OnPackageProject();
    static void OnMergeManagedSlnAndNativeSln();

    static void OnOpenSettings();
    static void OnOpenDocumentation();
    static void OnReportBug();
    
    void OnRefreshRuntimeGlue();

    static void OnRepairComponents();
    
    static void OnExploreArchiveDirectory(FString ArchiveDirectory);

    static void PackageProject();

    TSharedRef<SWidget> GenerateUnrealSharpMenu();
    
    static void OpenNewProjectDialog(const FString& SuggestedProjectName = FString());

    static void SuggestProjectSetup();
    
    bool Tick(float DeltaTime);
    
    void RegisterCommands();
    void RegisterMenu();
    void RegisterGameplayTags();
    void TryRegisterAssetTypes();
    void RegisterCollisionProfile();

    void OnAssetSearchRootAdded(const FString& RootPath);
    void OnCompletedInitialScan();

    bool IsRegisteredAssetType(const FAssetData& AssetData);
    bool IsRegisteredAssetType(UClass* Class);
    
    void OnAssetRemoved(const FAssetData& AssetData);
    void OnAssetRenamed(const FAssetData& AssetData, const FString& OldObjectPath);
    void OnInMemoryAssetCreated(UObject* Object);
    void OnInMemoryAssetDeleted(UObject* Object);

    void OnCollisionProfileLoaded(UCollisionProfile* Profile);
    void OnAssetManagerSettingsChanged(UObject* Object, FPropertyChangedEvent& PropertyChangedEvent);

    void OnPIEShutdown(bool IsSimulating);

    void WaitUpdateAssetTypes();

    void ProcessGameplayTags();
    void ProcessAssetIds();
    void ProcessAssetTypes();
    void ProcessTraceTypeQuery();
    
    void OnStructRebuilt(UCSScriptStruct* NewStruct);
    void OnClassRebuilt(UCSClass* NewClass);
    void OnEnumRebuilt(UCSEnum* NewEnum);

    bool IsPinAffectedByReload(const FEdGraphPinType& PinType) const;
    bool IsNodeAffectedByReload(UEdGraphNode* Node) const;

    void OnModulesChanged(FName InModuleName, EModuleChangeReason InModuleChangeReason);
    
    void RefreshAffectedBlueprints();

    FSlateIcon GetMenuIcon() const;

    static FString QuotePath(const FString& Path);

    FCSManagedUnrealSharpEditorCallbacks ManagedUnrealSharpEditorCallbacks;
    
    HotReloadStatus HotReloadStatus = Inactive;
    bool bHotReloadFailed = false;
    bool bHasQueuedHotReload = false;

    bool bHasRegisteredAssetTypes = false;

    FOnRefreshRuntimeGlue OnRefreshRuntimeGlueDelegate;

    TSharedPtr<FCSAssembly> EditorAssembly;
    FTickerDelegate TickDelegate;
    FTSTicker::FDelegateHandle TickDelegateHandle;
    TSharedPtr<FUICommandList> UnrealSharpCommands;
    
    TSet<UCSScriptStruct*> RebuiltStructs;
    TSet<UCSClass*> RebuiltClasses;
    TSet<UCSEnum*> RebuiltEnums;
    
    UCSManager* Manager = nullptr;
    bool bDirtyGlue = false;
};
