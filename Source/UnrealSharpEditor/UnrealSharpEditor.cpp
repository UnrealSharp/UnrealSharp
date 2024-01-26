#include "UnrealSharpEditor.h"
#include "DirectoryWatcherModule.h"
#include "IDirectoryWatcher.h"
#include "CSharpForUE/CSManager.h"
#include "CSharpForUE/CSDeveloperSettings.h"
#include "Misc/ScopedSlowTask.h"
#include "Reinstancing/CSReinstancer.h"

#define LOCTEXT_NAMESPACE "FUnrealSharpEditorModule"

void FUnrealSharpEditorModule::StartupModule()
{
	FDirectoryWatcherModule& DirectoryWatcherModule = FModuleManager::LoadModuleChecked<FDirectoryWatcherModule>("DirectoryWatcher");
	IDirectoryWatcher* DirectoryWatcher = DirectoryWatcherModule.Get();
	FDelegateHandle Handle;

	FString FullScriptPath = FPaths::ConvertRelativePathToFull(FPaths::ProjectDir() / "Script");

	if (!FPaths::DirectoryExists(FullScriptPath))
	{
		FPlatformFileManager::Get().GetPlatformFile().CreateDirectory(*FullScriptPath);
	}
	
	//Bind to directory watcher to look for changes in C# code.
	DirectoryWatcher->RegisterDirectoryChangedCallback_Handle(
		FullScriptPath,
		IDirectoryWatcher::FDirectoryChanged::CreateRaw(this, &FUnrealSharpEditorModule::OnCSharpCodeModified),
		Handle);

	FCSReinstancer::Get().Initialize();

	TickDelegate = FTickerDelegate::CreateRaw(this, &FUnrealSharpEditorModule::Tick);
	TickDelegateHandle = FTSTicker::GetCoreTicker().AddTicker(TickDelegate);

	ModuleLoadingPhaseCompleteDelegateHandle = FCoreDelegates::OnAllModuleLoadingPhasesComplete.AddRaw(this, &FUnrealSharpEditorModule::OnAllModuleLoadingPhasesComplete);
}

void FUnrealSharpEditorModule::ShutdownModule()
{
	FTSTicker::GetCoreTicker().RemoveTicker(TickDelegateHandle);
	FCoreDelegates::OnAllModuleLoadingPhasesComplete.Remove(ModuleLoadingPhaseCompleteDelegateHandle);
}

void FUnrealSharpEditorModule::OnCSharpCodeModified(const TArray<FFileChangeData>& ChangedFiles)
{
	if (bIsReloading)
	{
		return;
	}
	
	const UCSDeveloperSettings* Settings = GetDefault<UCSDeveloperSettings>();

	for (const FFileChangeData& ChangedFile : ChangedFiles)
	{
		// Skip generated files in bin and obj folders
		if (ChangedFile.Filename.Contains("Script/bin/") || ChangedFile.Filename.Contains("Script/obj/"))
		{
			continue;
		}

		// Check if the file is a .cs file
		FString Extension = FPaths::GetExtension(ChangedFile.Filename);
		if (Extension != "cs")
		{
			continue;
		}

		bIsReloading = true;

		// Return early and let TickDelegate handle Reload
		if (Settings->bRequireFocusForHotReload)
			return;

		// Return on the first .cs file we encounter so we can reload.
		Reload();
		bIsReloading = false;
		return;
	}
}

void FUnrealSharpEditorModule::Reload()
{
	FScopedSlowTask Progress(4, LOCTEXT("ReloadingCSharp", "Building C# code..."));
	Progress.MakeDialog();

	// Build the user's project.
	if (!FCSManager::InvokeUnrealSharpBuildTool(EBuildAction::Build))
	{
		return;
	}

	// Weave the user's project.
	Progress.EnterProgressFrame(1, LOCTEXT("WeavingCSharp", "Weaving C# code..."));
	if (!FCSManager::InvokeUnrealSharpBuildTool(EBuildAction::Weave))
	{
		return;
	}
	
	// Unload the user's assembly, to apply the new one.
	Progress.EnterProgressFrame(1, LOCTEXT("UnloadingAssembly", "Unloading Assembly..."));
	if (!FCSManager::Get().UnloadPlugin(FCSManager::UserManagedProjectName))
	{
		return;
	}

	// Load the user's assembly.
	Progress.EnterProgressFrame(1, LOCTEXT("LoadingAssembly", "Loading Assembly..."));
	if (!FCSManager::Get().LoadUserAssembly())
	{
		return;
	}

	// Reinstance all blueprints.
	Progress.EnterProgressFrame(1, LOCTEXT("ReinstancingBlueprints", "Reinstancing Blueprints..."));
	FCSReinstancer::Get().Reinstance();
}

bool FUnrealSharpEditorModule::Tick(float DeltaTime)
{
	const UCSDeveloperSettings* Settings = GetDefault<UCSDeveloperSettings>();
	if (!Settings->bRequireFocusForHotReload || !bIsReloading || !FApp::HasFocus()) return true;

	Reload();
	bIsReloading = false;

	return true;
}

void FUnrealSharpEditorModule::OnAllModuleLoadingPhasesComplete()
{
	Reload();
}

#undef LOCTEXT_NAMESPACE
    
IMPLEMENT_MODULE(FUnrealSharpEditorModule, UnrealSharpEditor)