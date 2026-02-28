#pragma once

#include "CoreMinimal.h"
#include "Modules/ModuleManager.h"
#include "Containers/Ticker.h"

#ifdef __clang__
#pragma clang diagnostic ignored "-Wignored-attributes"
#endif

class UCSManagedAssembly;
struct FPluginTemplateDescription;
enum ECSLoggerVerbosity : uint8;
class UCSInterface;
class UCSEnum;
class UCSClass;
class UCSScriptStruct;
class UCSManager;
class IAssetTools;
class FCSScriptBuilder;

struct FCSManagedEditorCallbacks
{
    FCSManagedEditorCallbacks() = default;
    
    using FRecompileDirtyProjects = bool(__stdcall*)(void*, TArray<FString>);
    using FRecompileChangedFile = void(__stdcall*)(const TCHAR*, const TCHAR*, void*);
    using FRemoveSourceFile = void(__stdcall*)(const TCHAR*, const TCHAR*);
    
    using FForceManagedGC = void(__stdcall*)();
    using FOpenSolution = bool(__stdcall*)(const TCHAR*, void*);
    using FLoadSignature = void(__stdcall*)(const TCHAR*, void*);

    FRecompileDirtyProjects RecompileDirtyProjects = nullptr;
    FRecompileChangedFile RecompileChangedFile = nullptr;
    FRemoveSourceFile RemoveSourceFile = nullptr;
    
    FForceManagedGC ForceManagedGC = nullptr;
    FOpenSolution OpenSolution = nullptr;
    
    FLoadSignature LoadSolutionAsync = nullptr;
    FLoadSignature LoadProject = nullptr;
};

DECLARE_LOG_CATEGORY_EXTERN(LogUnrealSharpEditor, Log, All);
DECLARE_MULTICAST_DELEGATE_OneParam(FCSOnBuildingToolbar, FMenuBuilder&);

class FUnrealSharpEditorModule : public IModuleInterface
{
public:
    UNREALSHARPEDITOR_API static FUnrealSharpEditorModule& Get();

    // IModuleInterface interface begin
    virtual void StartupModule() override;
    virtual void ShutdownModule() override;
    // End

    void InitializeManagedEditorCallbacks(FCSManagedEditorCallbacks Callbacks);
    FCSManagedEditorCallbacks& GetManagedEditorCallbacks() { return ManagedUnrealSharpEditorCallbacks; }

    UNREALSHARPEDITOR_API void AddNewProject(const FString& ModuleName, const FString& ProjectParentFolder, const FString& ProjectRoot, TMap<FString, FString>
                                             Arguments = {}, bool bOpenProject = true);
    
    UNREALSHARPEDITOR_API FCSOnBuildingToolbar& OnBuildingToolbarEvent() { return OnBuildingToolbar; }

private:
    
    static void SuggestProjectSetup();

    static FString SelectArchiveDirectory();

    static void RunGame(FString ExecutablePath);

    static void OnCreateNewProject();
    static void OnCompileManagedCode();
    
    void OnRegenerateSolution();
    void OnOpenSolution();
    void OpenSolution();
    
    static void OnPackageProject();
    static void OnMergeManagedSlnAndNativeSln();

    static void OnOpenSettings();
    static void OnOpenDocumentation();
    static void OnReportBug();
    
    static void OnExploreArchiveDirectory(FString ArchiveDirectory);
    static void PackageProject();

    TSharedRef<SWidget> GenerateUnrealSharpToolbar() const;

    static void OpenNewProjectDialog();

    void RegisterCommands();
    void RegisterToolbar();
    
    void RegisterPluginTemplates();
    void UnregisterPluginTemplates();

    void LoadNewProject(const FString& ModuleName, const FString& ModulePath) const;
    static void OnProjectLoaded();

    FCSManagedEditorCallbacks ManagedUnrealSharpEditorCallbacks;
    TSharedPtr<FUICommandList> UnrealSharpCommands;
    TArray<TSharedRef<FPluginTemplateDescription>> PluginTemplates;
    
    FCSOnBuildingToolbar OnBuildingToolbar;
};
