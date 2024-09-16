#include "UnrealSharpEditor.h"
#include "AssetToolsModule.h"
#include "CSCommands.h"
#include "DirectoryWatcherModule.h"
#include "CSStyle.h"
#include "DesktopPlatformModule.h"
#include "IDirectoryWatcher.h"
#include "ISettingsModule.h"
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

FUnrealSharpEditorModule& FUnrealSharpEditorModule::Get()
{
	return FModuleManager::LoadModuleChecked<FUnrealSharpEditorModule>("UnrealSharpEditor");
}

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

	FCSStyle::Initialize();

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
	if (IsHotReloading())
	{
		return;
	}
	
	const UCSDeveloperSettings* Settings = GetDefault<UCSDeveloperSettings>();

	for (const FFileChangeData& ChangedFile : ChangedFiles)
	{
		// Skip generated files in bin and obj folders
		if (ChangedFile.Filename.Contains("\\bin\\") || ChangedFile.Filename.Contains("\\obj\\"))
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
		if (Settings->AutomaticHotReloading != OnScriptSave)
		{
			HotReloadStatus = PendingReload;
		}
		else
		{
			StartHotReload();
		}
		
		return;
	}
}

void FUnrealSharpEditorModule::StartHotReload()
{
	TArray<FString> ProjectPaths;
	FCSProcHelper::GetAllProjectPaths(ProjectPaths);
	
	if (ProjectPaths.IsEmpty())
	{
		SuggestProjectSetup();
		return;
	}

	HotReloadStatus = Active;
	
	FScopedSlowTask Progress(2, LOCTEXT("HotReload", "Hot Reloading C#..."));
	Progress.MakeDialog();

	if (!FCSProcHelper::InvokeUnrealSharpBuildTool(BUILD_ACTION_BUILD_WEAVE))
	{
		HotReloadStatus = Inactive;
		bHotReloadFailed = true;
		return;
	}
	
	FCSManager& CSharpManager = FCSManager::Get();

	// Unload the user's assembly, to apply the new one.
	// TODO: Unload the assembly that was modified, not all of them, for sake of hot reload speed.
	for (const FString& ProjectPath : ProjectPaths)
	{
		FString ProjectName = FPaths::GetBaseFilename(ProjectPath);
		if (!CSharpManager.UnloadAssembly(ProjectName))
		{
			HotReloadStatus = Inactive;
			bHotReloadFailed = false;
			return;
		}
	}

	Progress.EnterProgressFrame(1, LOCTEXT("HotReload", "Loading C# Assembly..."));

	// TODO: Same here, only load the assembly that was modified.
	if (!CSharpManager.LoadUserAssembly())
	{
		HotReloadStatus = Inactive;
		bHotReloadFailed = true;
		return;
	}

	Progress.EnterProgressFrame(1, LOCTEXT("HotReload", "Reinstancing..."));
	FCSReinstancer::Get().StartReinstancing();

	HotReloadStatus = Inactive;
	bHotReloadFailed = false;
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

	TArray<FString> ProjectPaths;
	FCSProcHelper::GetAllProjectPaths(ProjectPaths);

	if (ProjectPaths.IsEmpty())
	{
		IMainFrameModule::Get().OnMainFrameCreationFinished().AddLambda([this](TSharedPtr<SWindow>, bool)
		{
			SuggestProjectSetup();
		});
	}
}

void FUnrealSharpEditorModule::OnCreateNewProject()
{
	OpenNewProjectDialog();
}

void FUnrealSharpEditorModule::OnCompileManagedCode()
{
	Get().StartHotReload();
}

void FUnrealSharpEditorModule::OnRegenerateSolution()
{
	if (!FCSProcHelper::InvokeUnrealSharpBuildTool(BUILD_ACTION_GENERATE_SOLUTION))
	{
		return;
	}

	OpenSolution();
}

void FUnrealSharpEditorModule::OnOpenSolution()
{
	OpenSolution();
}

void FUnrealSharpEditorModule::OnPackageProject()
{
	PackageProject();
}

void FUnrealSharpEditorModule::OnOpenSettings()
{
	FModuleManager::LoadModuleChecked<ISettingsModule>("Settings").ShowViewer("Editor", "General", "CSDeveloperSettings");
}

void FUnrealSharpEditorModule::OnOpenDocumentation()
{
	FPlatformProcess::LaunchURL(TEXT("https://www.unrealsharp.com"), nullptr, nullptr);
}

void FUnrealSharpEditorModule::OnReportBug()
{
	FPlatformProcess::LaunchURL(TEXT("https://github.com/UnrealSharp/UnrealSharp/issues"), nullptr, nullptr);
}

void FUnrealSharpEditorModule::PackageProject()
{
	FString ArchiveDirectory = SelectArchiveDirectory();

	if (ArchiveDirectory.IsEmpty())
	{
		return;
	}

	FString ExecutablePath = ArchiveDirectory / FApp::GetProjectName() + ".exe";
	if (!FPaths::FileExists(ExecutablePath))
	{
		FString DialogText = FString::Printf(TEXT("The executable for project '%s' could not be found in the directory: %s. Please select the root directory where you packaged your game."), FApp::GetProjectName(), *ArchiveDirectory);
		FMessageDialog::Open(EAppMsgType::Ok, FText::FromString(DialogText));
		return;
	}

	FScopedSlowTask Progress(1, LOCTEXT("USharpPackaging", "Packaging Project..."));
	Progress.MakeDialog();
	
	TMap<FString, FString> Arguments;
	Arguments.Add("ArchiveDirectory", QuotePath(ArchiveDirectory));
	Arguments.Add("BuildConfig", "Release");
	FCSProcHelper::InvokeUnrealSharpBuildTool(BUILD_ACTION_PACKAGE_PROJECT, Arguments);

	FNotificationInfo Info(FText::FromString(FString::Printf(TEXT("Project '%s' has been packaged successfully."), FApp::GetProjectName())));
	Info.ExpireDuration = 15.0f;
	Info.bFireAndForget = true;
	Info.ButtonDetails.Add(FNotificationButtonInfo(
			LOCTEXT("USharpRunPackagedGame", "Run Packaged Game"),
			LOCTEXT("", ""),
			FSimpleDelegate::CreateStatic(&FUnrealSharpEditorModule::RunGame, ExecutablePath),
			SNotificationItem::CS_None));

	Info.ButtonDetails.Add(FNotificationButtonInfo(
			LOCTEXT("USharpOpenPackagedGame", "Open Folder"),
			LOCTEXT("", ""),
			FSimpleDelegate::CreateStatic(&FPlatformProcess::ExploreFolder, *ArchiveDirectory),
			SNotificationItem::CS_None));

	TSharedPtr<SNotificationItem> NotificationItem = FSlateNotificationManager::Get().AddNotification(Info);
	NotificationItem->SetCompletionState(SNotificationItem::CS_None);
}

void FUnrealSharpEditorModule::RunGame(FString ExecutablePath)
{
	FString OpenSolutionArgs = FString::Printf(TEXT("/c \"%s\""), *ExecutablePath);
	FPlatformProcess::ExecProcess(TEXT("cmd.exe"), *OpenSolutionArgs, nullptr, nullptr, nullptr);
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
};

FString FUnrealSharpEditorModule::SelectArchiveDirectory()
{
	IDesktopPlatform* DesktopPlatform = FDesktopPlatformModule::Get();
	if(!DesktopPlatform)
	{
		return FString();
	}

	FString DestinationFolder;
	const void* ParentWindowHandle = FSlateApplication::Get().FindBestParentWindowHandleForDialogs(nullptr);
	const FString Title = LOCTEXT("USharpChooseArchiveRoot", "Find Archive Root").ToString();

	if(DesktopPlatform->OpenDirectoryDialog(ParentWindowHandle, Title, FString(), DestinationFolder))
	{
		return FPaths::ConvertRelativePathToFull(DestinationFolder);
	}

	return FString();
}

TSharedRef<SWidget> FUnrealSharpEditorModule::GenerateUnrealSharpMenu()
{
	const FCSCommands& CSCommands = FCSCommands::Get();
	FMenuBuilder MenuBuilder(true, UnrealSharpCommands);

	// Build
	MenuBuilder.BeginSection("Build", LOCTEXT("Build", "Build"));
	
	MenuBuilder.AddMenuEntry(CSCommands.CompileManagedCode, NAME_None, TAttribute<FText>(), TAttribute<FText>(),
		FSlateIcon(FAppStyle::Get().GetStyleSetName(), "LevelEditor.Recompile"));
	
	MenuBuilder.EndSection();

	// Project
	MenuBuilder.BeginSection("Project", LOCTEXT("Project", "Project"));
	
	MenuBuilder.AddMenuEntry(CSCommands.CreateNewProject, NAME_None, TAttribute<FText>(), TAttribute<FText>(),
		FSourceCodeNavigation::GetOpenSourceCodeIDEIcon());

	MenuBuilder.AddMenuEntry(CSCommands.OpenSolution, NAME_None, TAttribute<FText>(), TAttribute<FText>(),
		FSourceCodeNavigation::GetOpenSourceCodeIDEIcon());
	
	MenuBuilder.AddMenuEntry(CSCommands.RegenerateSolution, NAME_None, TAttribute<FText>(), TAttribute<FText>(),
		FSourceCodeNavigation::GetOpenSourceCodeIDEIcon());
	
	MenuBuilder.EndSection();

	// Package
	MenuBuilder.BeginSection("Package", LOCTEXT("Package", "Package"));

	MenuBuilder.AddMenuEntry(CSCommands.PackageProject, NAME_None, TAttribute<FText>(), TAttribute<FText>(),
		FSlateIcon(FAppStyle::Get().GetStyleSetName(), "LevelEditor.Recompile"));

	MenuBuilder.EndSection();

	// Plugin
	MenuBuilder.BeginSection("Plugin", LOCTEXT("Plugin", "Plugin"));

	MenuBuilder.AddMenuEntry(CSCommands.OpenSettings, NAME_None, TAttribute<FText>(), TAttribute<FText>(),
		FSlateIcon(FAppStyle::Get().GetStyleSetName(), "EditorPreferences.TabIcon"));

	MenuBuilder.AddMenuEntry(CSCommands.OpenDocumentation, NAME_None, TAttribute<FText>(), TAttribute<FText>(),
		FSlateIcon(FAppStyle::Get().GetStyleSetName(), "MainFrame.DocumentationHome"));

	MenuBuilder.AddMenuEntry(CSCommands.ReportBug, NAME_None, TAttribute<FText>(), TAttribute<FText>(),
		FSlateIcon(FAppStyle::Get().GetStyleSetName(), "MainFrame.ReportABug"));

	MenuBuilder.EndSection();
	
	return MenuBuilder.MakeWidget();
}

void FUnrealSharpEditorModule::OpenNewProjectDialog(const FString& SuggestedProjectName)
{
	TSharedRef<SWindow> AddCodeWindow = SNew(SWindow)
	.Title(LOCTEXT("CreateNewProject", "New C# Project"))
	.SizingRule( ESizingRule::Autosized )
	.SupportsMinimize(false);

	TSharedRef<SCSNewProjectDialog> NewProjectDialog = SNew(SCSNewProjectDialog)
		.SuggestedProjectName(SuggestedProjectName);
	
	AddCodeWindow->SetContent(NewProjectDialog);

	FSlateApplication::Get().AddWindow(AddCodeWindow);
}

void FUnrealSharpEditorModule::SuggestProjectSetup()
{
	FString DialogText = TEXT("No C# projects were found. Would you like to create a new C# project?");
	EAppReturnType::Type Result = FMessageDialog::Open(EAppMsgType::YesNo, FText::FromString(DialogText));
	
	if (Result == EAppReturnType::No)
	{
		return;
	}

	FString SuggestedProjectName = FString::Printf(TEXT("Managed%s"), FApp::GetProjectName());
	OpenNewProjectDialog(SuggestedProjectName);
}

bool FUnrealSharpEditorModule::Tick(float DeltaTime)
{
	const UCSDeveloperSettings* Settings = GetDefault<UCSDeveloperSettings>();
	if (Settings->AutomaticHotReloading == OnEditorFocus && !IsHotReloading() && HasPendingHotReloadChanges() && FApp::HasFocus())
	{
		StartHotReload();
	}

	return true;
}

void FUnrealSharpEditorModule::RegisterCommands()
{
	FCSCommands::Register();
	UnrealSharpCommands = MakeShareable(new FUICommandList);
	UnrealSharpCommands->MapAction(FCSCommands::Get().CreateNewProject, FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnCreateNewProject));
	UnrealSharpCommands->MapAction(FCSCommands::Get().CompileManagedCode, FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnCompileManagedCode));
	UnrealSharpCommands->MapAction(FCSCommands::Get().RegenerateSolution, FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnRegenerateSolution));
	UnrealSharpCommands->MapAction(FCSCommands::Get().OpenSolution, FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnOpenSolution));
	UnrealSharpCommands->MapAction(FCSCommands::Get().PackageProject, FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnPackageProject));
	UnrealSharpCommands->MapAction(FCSCommands::Get().OpenSettings, FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnOpenSettings));
	UnrealSharpCommands->MapAction(FCSCommands::Get().OpenDocumentation, FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnOpenDocumentation));
	UnrealSharpCommands->MapAction(FCSCommands::Get().ReportBug, FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnReportBug));
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
	TAttribute<FSlateIcon>::CreateLambda([this]()
	{
		return GetMenuIcon();
	}));

	Section.AddEntry(Entry);
}

FSlateIcon FUnrealSharpEditorModule::GetMenuIcon() const
{
	if (HasHotReloadFailed())
	{
		return FSlateIcon(FCSStyle::GetStyleSetName(), "UnrealSharp.Toolbar.Fail"); 
	}
	if (HasPendingHotReloadChanges())
	{
		return FSlateIcon(FCSStyle::GetStyleSetName(), "UnrealSharp.Toolbar.Modified"); 
	}
	
	return FSlateIcon(FCSStyle::GetStyleSetName(), "UnrealSharp.Toolbar");
}

FString FUnrealSharpEditorModule::QuotePath(const FString& Path)
{
	return FString::Printf(TEXT("\"%s\""), *Path);
}

#undef LOCTEXT_NAMESPACE
    
IMPLEMENT_MODULE(FUnrealSharpEditorModule, UnrealSharpEditor)
