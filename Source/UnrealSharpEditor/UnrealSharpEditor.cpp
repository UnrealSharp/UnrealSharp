#include "UnrealSharpEditor.h"
#include "DirectoryWatcherModule.h"
#include "IDirectoryWatcher.h"
#include "CSharpForUE/CSManager.h"
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
}

void FUnrealSharpEditorModule::ShutdownModule()
{
    
}

void FUnrealSharpEditorModule::OnCSharpCodeModified(const TArray<FFileChangeData>& ChangedFiles)
{
	if (bIsReloading)
	{
		return;
	}
	
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
		
		// Return on the first .cs file we encounter so we can reload.
		bIsReloading = true;
		Reload();
		bIsReloading = false;
		return;
	}
}

void FUnrealSharpEditorModule::Reload()
{
	FScopedSlowTask Progress(4, LOCTEXT("BuildCSharp", "Building C# code..."));
	Progress.MakeDialog();

	// Build the user's project.
	if (!FCSManager::InvokeUnrealSharpBuildTool(EBuildAction::Build))
	{
		return;
	}

	// Weave the user's project.
	if (!FCSManager::InvokeUnrealSharpBuildTool(EBuildAction::Weave))
	{
		return;
	}
	
	// Unload the user's assembly, to apply the new one.
	if (!FCSManager::Get().UnloadPlugin(FCSManager::UserManagedProjectName))
	{
		return;
	}
	
	if (!FCSManager::Get().LoadUserAssembly())
	{
		return;
	}

	Progress.EnterProgressFrame(1, LOCTEXT("ReinstancingBlueprints", "Reinstancing Blueprints..."));
	FCSReinstancer::Get().Reinstance();
}

#undef LOCTEXT_NAMESPACE
    
IMPLEMENT_MODULE(FUnrealSharpEditorModule, UnrealSharpEditor)