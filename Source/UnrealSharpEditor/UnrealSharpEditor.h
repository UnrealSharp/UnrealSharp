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

    FUICommandList& GetUnrealSharpCommands() const { return *UnrealSharpCommands; }
    
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
    static void OnRefreshRuntimeGlue();

    static void OnRepairComponents();
    
    static void OnExploreArchiveDirectory(FString ArchiveDirectory);

    static void PackageProject();

    TSharedRef<SWidget> GenerateUnrealSharpMenu();
    
    static void OpenNewProjectDialog(const FString& SuggestedProjectName = FString());

    static void SuggestProjectSetup();
    
    bool Tick(float DeltaTime);
    
    void RegisterCommands();
    void RegisterMenu();

    void OnPIEShutdown(bool IsSimulating);
    
    void OnStructRebuilt(UCSScriptStruct* NewStruct);
    void OnClassRebuilt(UCSClass* NewClass);
    void OnEnumRebuilt(UCSEnum* NewEnum);

    bool IsPinAffectedByReload(const FEdGraphPinType& PinType) const;
    bool IsNodeAffectedByReload(UEdGraphNode* Node) const;
    
    void RefreshAffectedBlueprints();

    FSlateIcon GetMenuIcon() const;

    static FString QuotePath(const FString& Path);

    FCSManagedUnrealSharpEditorCallbacks ManagedUnrealSharpEditorCallbacks;
    
    HotReloadStatus HotReloadStatus = Inactive;
    bool bHotReloadFailed = false;
    bool bHasQueuedHotReload = false;

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
