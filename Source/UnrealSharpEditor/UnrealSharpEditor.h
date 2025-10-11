#pragma once

#include "CoreMinimal.h"
#include "Modules/ModuleManager.h"
#include "Containers/Ticker.h"

#ifdef __clang__
#pragma clang diagnostic ignored "-Wignored-attributes"
#endif

class UCSAssembly;
struct FPluginTemplateDescription;
enum ECSLoggerVerbosity : uint8;
class UCSInterface;
class UCSEnum;
class UCSClass;
class UCSScriptStruct;
class UCSManager;
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
    FCSManagedUnrealSharpEditorCallbacks() : RunGeneratorsAndEmitAsync(nullptr), ForceManagedGC(nullptr), OpenSolution(nullptr), LoadSolution(nullptr)
    {
    }

    using FRunGeneratorsAndEmitAsync = bool(__stdcall*)(void*);
    using FDirtyFile = void(__stdcall*)(const TCHAR*, const TCHAR*, void*);
    using FForceManagedGC = void(__stdcall*)();
    using FOpenSolution = bool(__stdcall*)(const TCHAR*, void*);
    using FAddProjectToCollection = void(__stdcall*)(const TCHAR*, void*);
     using FGetDependentProjects = void(__stdcall*)(const TCHAR*, TArray<FString>*);

    FRunGeneratorsAndEmitAsync RunGeneratorsAndEmitAsync;
    FDirtyFile DirtyFilesCallback;
    FForceManagedGC ForceManagedGC;
    FOpenSolution OpenSolution;
    FAddProjectToCollection LoadSolution;
    FGetDependentProjects GetDependentProjects;
    
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

    void InitializeUnrealSharpEditorCallbacks(FCSManagedUnrealSharpEditorCallbacks Callbacks);

    FUICommandList& GetUnrealSharpCommands() const { return *UnrealSharpCommands; }

    void OpenSolution();

    void AddNewProject(const FString& ModuleName, const FString& ProjectParentFolder, const FString& ProjectRoot, const TMap<FString, FString>& Arguments = {});

    FCSManagedUnrealSharpEditorCallbacks& GetManagedUnrealSharpEditorCallbacks()
    {
        return ManagedUnrealSharpEditorCallbacks;
    }
    
    static bool FillTemplateFile(const FString& TemplateName, TMap<FString, FString>& Replacements, const FString& Path);
    static void SuggestProjectSetup();

private:

    static FString SelectArchiveDirectory();

    static void RunGame(FString ExecutablePath);

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
    
    static void OnExploreArchiveDirectory(FString ArchiveDirectory);
    static void PackageProject();

    TSharedRef<SWidget> GenerateUnrealSharpMenu();

    static void OpenNewProjectDialog();

    void RegisterCommands();
    void RegisterMenu();
    void RegisterPluginTemplates();
    void UnregisterPluginTemplates();

    FCSManagedUnrealSharpEditorCallbacks ManagedUnrealSharpEditorCallbacks;

    UCSAssembly* EditorAssembly = nullptr;
    TSharedPtr<FUICommandList> UnrealSharpCommands;
    TArray<TSharedRef<FPluginTemplateDescription>> PluginTemplates;

    UCSManager* Manager = nullptr;
};
