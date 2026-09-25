#pragma once

#include "CoreMinimal.h"
#include "CSEditorCommands.h"
#include "Modules/ModuleManager.h"
#include "Containers/Ticker.h"
#include "CSInteropTypeTraits.h"

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

// Trivially-copyable mirror of the managed UnmanagedArray struct. A TArray passed by value is
// non-trivial, so the SysV x86-64 ABI (Linux/Mac) passes it by hidden reference while .NET passes
// the 16-byte managed struct in registers; this POD matches the managed side on every platform.
struct FCSUnmanagedArrayView
{
    const void* Data = nullptr;
    int32 ArrayNum = 0;
    int32 ArrayMax = 0;
};

struct FCSManagedEditorCallbacks
{
    FCSManagedEditorCallbacks() = default;
    
    using FRecompileDirtyProjects = bool(__stdcall*)(void*, FCSUnmanagedArrayView);
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

// Filled in by managed code (FManagedUnrealSharpEditorCallbacks) and passed across the boundary by value.
CS_ASSERT_INTEROP_SAFE_TYPE(FCSUnmanagedArrayView);
CS_ASSERT_INTEROP_SAFE_TYPE(FCSManagedEditorCallbacks);
CS_ASSERT_INTEROP_SAFE_FUNCTION(FCSManagedEditorCallbacks::FRecompileDirtyProjects);
CS_ASSERT_INTEROP_SAFE_FUNCTION(FCSManagedEditorCallbacks::FRecompileChangedFile);
CS_ASSERT_INTEROP_SAFE_FUNCTION(FCSManagedEditorCallbacks::FRemoveSourceFile);
CS_ASSERT_INTEROP_SAFE_FUNCTION(FCSManagedEditorCallbacks::FForceManagedGC);
CS_ASSERT_INTEROP_SAFE_FUNCTION(FCSManagedEditorCallbacks::FOpenSolution);
CS_ASSERT_INTEROP_SAFE_FUNCTION(FCSManagedEditorCallbacks::FLoadSignature);

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

    UNREALSHARPEDITOR_API void AddNewProject(const FString& ModuleName, const FString& ProjectParentFolder, const FString& ProjectRoot, TMap<FString, FString> Arguments = {}, bool bOpenProject = true);
    UNREALSHARPEDITOR_API FCSOnBuildingToolbar& OnBuildingToolbarEvent() { return OnBuildingToolbar; }

private:
    
    static void SuggestProjectSetup();

    static FString SelectArchiveDirectory();

    static void OnCreateNewProject();
    static void OnCompileManagedCode();
    
    void OnCreateNewClass();
    void HandleNewClassCreated(const FString& ClassName, const FString& FilePath);
    
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
    
    static void AppendProjectMenu(const FCSEditorCommands& CSCommands, FMenuBuilder& MenuBuilder);
    static void AppendPackageMenu(const FCSEditorCommands& CSCommands, FMenuBuilder& MenuBuilder);
    static void AppendCodeMenu(const FCSEditorCommands& CSCommands, FMenuBuilder& MenuBuilder);
    static void AppendBuildMenu(const FCSEditorCommands& CSCommands, FMenuBuilder& MenuBuilder);
    static void AppendPluginMenu(const FCSEditorCommands& CSCommands, FMenuBuilder& MenuBuilder);

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
