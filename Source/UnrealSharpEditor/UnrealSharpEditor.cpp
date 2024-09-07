#include "UnrealSharpEditor.h"

#include "AssetToolsModule.h"
#include "CSCommands.h"
#include "DirectoryWatcherModule.h"
#include "IDirectoryWatcher.h"
#include "SourceCodeNavigation.h"
#include "CSharpForUE/CSManager.h"
#include "CSharpForUE/CSDeveloperSettings.h"
#include "Framework/Notifications/NotificationManager.h"
#include "Interfaces/IMainFrameModule.h"
#include "Misc/ScopedSlowTask.h"
#include "Reinstancing/CSReinstancer.h"
#include "Slate/CSNewProjectWizard.h"
#include "UnrealSharpProcHelper/CSProcHelper.h"
#include "Widgets/Notifications/SNotificationList.h"

#define LOCTEXT_NAMESPACE "FUnrealSharpEditorModule"

void FUnrealSharpEditorModule::StartupModule()
{
	FCSManager& Manager = FCSManager::Get();
	if (!Manager.IsInitialized())
	{
		Manager.OnUnrealSharpInitializedEvent().AddRaw(this, &FUnrealSharpEditorModule::OnUnrealSharpInitialized);
	}
	else
	{
		OnUnrealSharpInitialized();
	}

	RegisterCommands();
	RegisterMenu();
}

void FUnrealSharpEditorModule::ShutdownModule()
{
	FTSTicker::GetCoreTicker().RemoveTicker(TickDelegateHandle);
	UToolMenus::UnRegisterStartupCallback(this);
	UToolMenus::UnregisterOwner(this);
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
		if (ChangedFile.Filename.Contains("Script/bin") || ChangedFile.Filename.Contains("Script/obj"))
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

		// Return early and let TickDelegate handle Reload
		if (Settings->bRequireFocusForHotReload)
		{
			return;
		}
		
		StartHotReload();
		bIsReloading = false;
		return;
	}
}

void FUnrealSharpEditorModule::StartHotReload()
{
	FScopedSlowTask Progress(4, LOCTEXT("ReloadingCSharp", "Building C# code..."));
	Progress.MakeDialog();

	// Build the user's project.
	if (!FCSProcHelper::InvokeUnrealSharpBuildTool(Build))
	{
		return;
	}

	// Weave the user's project.
	Progress.EnterProgressFrame(1, LOCTEXT("WeavingCSharp", "Weaving C# code..."));
	if (!FCSProcHelper::InvokeUnrealSharpBuildTool(Weave))
	{
		return;
	}
	
	// Unload the user's assembly, to apply the new one.
	Progress.EnterProgressFrame(1, LOCTEXT("UnloadingAssembly", "Unloading Assembly..."));
	if (!FCSManager::Get().UnloadAssembly(FCSProcHelper::GetUserManagedProjectName()))
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
	FCSReinstancer::Get().StartReinstancing();
}

void FUnrealSharpEditorModule::OnUnrealSharpInitialized()
{
	FCSManager& Manager = FCSManager::Get();
	
	// Deny any classes from being Edited in BP that's in the UnrealSharp package. Otherwise it would crash the engine.
	// Workaround for a hardcoded feature in the engine for Blueprints.
	FAssetToolsModule& AssetToolsModule = FModuleManager::LoadModuleChecked<FAssetToolsModule>(TEXT("AssetTools"));
	FName UnrealSharpPackageName = Manager.GetUnrealSharpPackage()->GetFName();
	AssetToolsModule.Get().GetWritableFolderPermissionList()->AddDenyListItem(UnrealSharpPackageName, UnrealSharpPackageName);

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
	
	IMainFrameModule::Get().OnMainFrameCreationFinished().AddLambda([this](TSharedPtr<SWindow>, bool)
	{
		CheckIfSetupIsNeeded();
	});
}

void FUnrealSharpEditorModule::OnCreateNewProject()
{
	OpenNewProjectDialog();
}

void FUnrealSharpEditorModule::OnRegenerateSolution()
{
	if (!FCSProcHelper::InvokeUnrealSharpBuildTool(GenerateSolution))
	{
		return;
	}

	FText Message = LOCTEXT("SolutionRegenerated", "C# solution regenerated successfully!");
	FSlateNotificationManager::Get().AddNotification(FNotificationInfo(Message));
}

void FUnrealSharpEditorModule::OpenSolution()
{
	FString SolutionPath = FPaths::ConvertRelativePathToFull(FCSProcHelper::GetPathToSolution());

	if (!FPaths::FileExists(SolutionPath))
	{
		OnRegenerateSolution();
	}
	
	FString OpenSolutionArgs = FString::Printf(TEXT("/c \"%s\""), *SolutionPath);
	FPlatformProcess::ExecProcess(TEXT("cmd.exe"), *OpenSolutionArgs, nullptr, nullptr, nullptr);
}

TSharedRef<SWidget> FUnrealSharpEditorModule::GenerateUnrealSharpMenu()
{
	const FCSCommands& CSCommands = FCSCommands::Get();
	FMenuBuilder MenuBuilder(false, UnrealSharpCommands);
	
	MenuBuilder.BeginSection("Build", LOCTEXT("Build", "Build"));
	
	MenuBuilder.AddMenuEntry(CSCommands.CompileManagedCode, NAME_None, TAttribute<FText>(), TAttribute<FText>(),
		FSlateIcon(FAppStyle::Get().GetStyleSetName(), "LevelEditor.Recompile"));
	
	MenuBuilder.EndSection();

	MenuBuilder.BeginSection("Project", LOCTEXT("Project", "Project"));
	
	MenuBuilder.AddMenuEntry(CSCommands.CreateNewProject, NAME_None, TAttribute<FText>(), TAttribute<FText>(),
		FSourceCodeNavigation::GetOpenSourceCodeIDEIcon());

	MenuBuilder.AddMenuEntry(CSCommands.OpenSolution, NAME_None, TAttribute<FText>(), TAttribute<FText>(),
		FSourceCodeNavigation::GetOpenSourceCodeIDEIcon());
	
	MenuBuilder.AddMenuEntry(CSCommands.RegenerateSolution, NAME_None, TAttribute<FText>(), TAttribute<FText>(),
		FSourceCodeNavigation::GetOpenSourceCodeIDEIcon());
	
	MenuBuilder.EndSection();
	
	return MenuBuilder.MakeWidget();
}

void FUnrealSharpEditorModule::OpenNewProjectDialog(const FString& SuggestedProjectName, bool bOpenSolution)
{
	TSharedRef<SWindow> AddCodeWindow = SNew(SWindow)
	.Title(LOCTEXT("CreateNewProject", "New C# Project"))
	.SizingRule( ESizingRule::Autosized )
	.SupportsMinimize(false);

	TSharedRef<SCSNewProjectDialog> NewProjectDialog = SNew(SCSNewProjectDialog)
		.SuggestedProjectName(SuggestedProjectName)
		.OpenSolution(bOpenSolution);
	
	AddCodeWindow->SetContent(NewProjectDialog);

	FSlateApplication::Get().AddWindow(AddCodeWindow);
}

void FUnrealSharpEditorModule::CheckIfSetupIsNeeded()
{
	TArray<FString> AssemblyPaths;
	IFileManager::Get().FindFilesRecursive(AssemblyPaths, *FCSProcHelper::GetScriptFolderDirectory(), TEXT("*.csproj"), true, false, false);

	if (!AssemblyPaths.IsEmpty())
	{
		return;
	}

	FString DialogText = TEXT("No C# projects were found. Would you like to create a new C# project?");
	EAppReturnType::Type Result = FMessageDialog::Open(EAppMsgType::YesNo, FText::FromString(DialogText));
	
	if (Result == EAppReturnType::No)
	{
		return;
	}

	FString SuggestedProjectName = FString::Printf(TEXT("Managed%s"), FApp::GetProjectName());
	OpenNewProjectDialog(SuggestedProjectName, true);
}

bool FUnrealSharpEditorModule::Tick(float DeltaTime)
{
	const UCSDeveloperSettings* Settings = GetDefault<UCSDeveloperSettings>();
	if (!Settings->bRequireFocusForHotReload || !bIsReloading || !FApp::HasFocus())
	{
		return true;
	}

	StartHotReload();
	bIsReloading = false;
	return true;
}

void FUnrealSharpEditorModule::RegisterCommands()
{
	FCSCommands::Register();
	UnrealSharpCommands = MakeShareable(new FUICommandList);
	UnrealSharpCommands->MapAction(FCSCommands::Get().CreateNewProject, FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnCreateNewProject));
	UnrealSharpCommands->MapAction(FCSCommands::Get().CompileManagedCode, FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::StartHotReload));
	UnrealSharpCommands->MapAction(FCSCommands::Get().RegenerateSolution, FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnRegenerateSolution));
	UnrealSharpCommands->MapAction(FCSCommands::Get().OpenSolution, FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OpenSolution));
}

void FUnrealSharpEditorModule::RegisterMenu()
{
	UToolMenu* ToolbarMenu = UToolMenus::Get()->ExtendMenu("LevelEditor.LevelEditorToolBar.PlayToolBar");
	FToolMenuSection& Section = ToolbarMenu->FindOrAddSection("PluginTools");
	
	FToolMenuEntry Entry = FToolMenuEntry::InitComboButton(
	"UnrealSharp",
	FUIAction(),
	FOnGetContent::CreateLambda([this](){ return GenerateUnrealSharpMenu(); }),
	LOCTEXT("UnrealSharp_Label", "UnrealSharp"),
	LOCTEXT("UnrealSharp_Tooltip", "List of all UnrealSharp actions"),
	FSlateIcon(FAppStyle::Get().GetStyleSetName(), "PlayWorld.RepeatLastLaunch"));

	Section.AddEntry(Entry);
}

#undef LOCTEXT_NAMESPACE
    
IMPLEMENT_MODULE(FUnrealSharpEditorModule, UnrealSharpEditor)
