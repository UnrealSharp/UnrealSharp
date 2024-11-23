#include "UnrealSharpEditor.h"
#include "AssetToolsModule.h"
#include "CSCommands.h"
#include "CSScriptBuilder.h"
#include "DirectoryWatcherModule.h"
#include "CSStyle.h"
#include "DesktopPlatformModule.h"
#include "GameplayTagsModule.h"
#include "GameplayTagsSettings.h"
#include "IDirectoryWatcher.h"
#include "ISettingsModule.h"
#include "LevelEditor.h"
#include "SourceCodeNavigation.h"
#include "Engine/AssetManager.h"
#include "Engine/AssetManagerSettings.h"
#include "UnrealSharpCore/CSManager.h"
#include "UnrealSharpCore/CSDeveloperSettings.h"
#include "Framework/Notifications/NotificationManager.h"
#include "Interfaces/IMainFrameModule.h"
#include "Misc/ScopedSlowTask.h"
#include "Reinstancing/CSReinstancer.h"
#include "Slate/CSNewProjectWizard.h"
#include "UnrealSharpProcHelper/CSProcHelper.h"
#include "Widgets/Notifications/SNotificationList.h"

#define LOCTEXT_NAMESPACE "FUnrealSharpEditorModule"

DEFINE_LOG_CATEGORY(LogUnrealSharpEditor);

FUnrealSharpEditorModule& FUnrealSharpEditorModule::Get()
{
	return FModuleManager::LoadModuleChecked<FUnrealSharpEditorModule>("UnrealSharpEditor");
}

void FUnrealSharpEditorModule::StartupModule()
{
	UCSManager& Manager = UCSManager::GetOrCreate();
	
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

	FCSStyle::Initialize();

	RegisterCommands();
	RegisterMenu();
	RegisterGameplayTags();
	RegisterAssetTypes();
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
		FString NormalizedFileName = ChangedFile.Filename;
		FPaths::NormalizeFilename(NormalizedFileName);
		
		// Skip ProjectGlue files
		if (NormalizedFileName.Contains(TEXT("ProjectGlue")))
		{
			continue;
		}
		
		// Skip generated files in bin and obj folders
		if (NormalizedFileName.Contains(TEXT("/obj/")))
		{
			continue;
		}

		if (Settings->AutomaticHotReloading == OnModuleChange && NormalizedFileName.EndsWith(".dll") && NormalizedFileName.Contains(TEXT("/bin/")))
		{
			// A module changed, initiate the reload and return
			StartHotReload(false);
			return;
		}
		
		// Check if the file is a .cs file and not in the bin directory
		FString Extension = FPaths::GetExtension(NormalizedFileName);
		if (Extension != "cs" || NormalizedFileName.Contains(TEXT("/bin/")))
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

void FUnrealSharpEditorModule::StartHotReload(bool bRebuild)
{
	TArray<FString> ProjectPaths;
	FCSProcHelper::GetAllProjectPaths(ProjectPaths);
	
	if (ProjectPaths.IsEmpty())
	{
		SuggestProjectSetup();
		return;
	}

	HotReloadStatus = Active;
	
	FScopedSlowTask Progress(3, LOCTEXT("HotReload", "Hot Reloading C#..."));
	Progress.MakeDialog();

	if (bRebuild)
	{
		Progress.EnterProgressFrame(1, LOCTEXT("HotReload", "Building C# Project..."));
		if(!FCSProcHelper::InvokeUnrealSharpBuildTool(BUILD_ACTION_BUILD_WEAVE))
		{
			HotReloadStatus = Inactive;
			bHotReloadFailed = true;
			return;
		}
	}
	else
	{
		Progress.EnterProgressFrame(1, LOCTEXT("HotReload", "Weaving C# Project..."));
		if(!FCSProcHelper::InvokeUnrealSharpBuildTool(BUILD_ACTION_WEAVE))
		{
			HotReloadStatus = Inactive;
			bHotReloadFailed = true;
			return;
		}
	}
	
	UCSManager& CSharpManager = UCSManager::Get();

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
	if (!CSharpManager.LoadAllUserAssemblies())
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

void FUnrealSharpEditorModule::OnCreateNewProject()
{
	OpenNewProjectDialog();
}

void FUnrealSharpEditorModule::OnCompileManagedCode()
{
	Get().StartHotReload();
}

void FUnrealSharpEditorModule::OnReloadManagedCode()
{
	Get().StartHotReload(false);
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

void FUnrealSharpEditorModule::OnExploreArchiveDirectory(FString ArchiveDirectory)
{
	FPlatformProcess::ExploreFolder(*ArchiveDirectory);
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
			FSimpleDelegate::CreateStatic(&FUnrealSharpEditorModule::OnExploreArchiveDirectory, ArchiveDirectory),
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

	MenuBuilder.AddMenuEntry(CSCommands.ReloadManagedCode, NAME_None, TAttribute<FText>(), TAttribute<FText>(),
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
	UnrealSharpCommands->MapAction(FCSCommands::Get().ReloadManagedCode, FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnReloadManagedCode));
	UnrealSharpCommands->MapAction(FCSCommands::Get().RegenerateSolution, FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnRegenerateSolution));
	UnrealSharpCommands->MapAction(FCSCommands::Get().OpenSolution, FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnOpenSolution));
	UnrealSharpCommands->MapAction(FCSCommands::Get().PackageProject, FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnPackageProject));
	UnrealSharpCommands->MapAction(FCSCommands::Get().OpenSettings, FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnOpenSettings));
	UnrealSharpCommands->MapAction(FCSCommands::Get().OpenDocumentation, FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnOpenDocumentation));
	UnrealSharpCommands->MapAction(FCSCommands::Get().ReportBug, FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnReportBug));

	const FLevelEditorModule& LevelEditorModule = FModuleManager::GetModuleChecked<FLevelEditorModule>("LevelEditor");
	const TSharedRef<FUICommandList> Commands = LevelEditorModule.GetGlobalLevelEditorActions();
	Commands->Append(UnrealSharpCommands.ToSharedRef());
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

void FUnrealSharpEditorModule::RegisterGameplayTags()
{
	IGameplayTagsModule::OnTagSettingsChanged.AddStatic(&FUnrealSharpEditorModule::ProcessGameplayTags);
	IGameplayTagsModule::OnGameplayTagTreeChanged.AddStatic(&FUnrealSharpEditorModule::ProcessGameplayTags);
	ProcessGameplayTags();
}

void FUnrealSharpEditorModule::RegisterAssetTypes()
{
	UAssetManager::Get().CallOrRegister_OnCompletedInitialScan(
		FSimpleMulticastDelegate::FDelegate::CreateStatic(&FUnrealSharpEditorModule::OnCompletedInitialScan));
}

void FUnrealSharpEditorModule::SaveRuntimeGlue(const FCSScriptBuilder& ScriptBuilder, const FString& FileName)
{
	const FString Path = FPaths::Combine(FCSProcHelper::GetScriptFolderDirectory(), TEXT("ProjectGlue"), FileName + TEXT(".cs"));
	if (!FFileHelper::SaveStringToFile(ScriptBuilder.ToString(), *Path))
	{
		UE_LOG(LogUnrealSharpEditor, Error, TEXT("Failed to save %s"), *FileName);
	}
}

void FUnrealSharpEditorModule::OnCompletedInitialScan()
{
	IAssetRegistry& AssetRegistry = FModuleManager::LoadModuleChecked<FAssetRegistryModule>("AssetRegistry").Get();
	AssetRegistry.OnAssetRemoved().AddStatic(&FUnrealSharpEditorModule::OnAssetRemoved);
	AssetRegistry.OnAssetRenamed().AddStatic(&FUnrealSharpEditorModule::OnAssetRenamed);
	AssetRegistry.OnInMemoryAssetCreated().AddStatic(&FUnrealSharpEditorModule::OnInMemoryAssetCreated);
	AssetRegistry.OnInMemoryAssetDeleted().AddStatic(&FUnrealSharpEditorModule::OnInMemoryAssetDeleted);
	
	UAssetManager::Get().Register_OnAddedAssetSearchRoot(
		FOnAddedAssetSearchRoot::FDelegate::CreateStatic(&FUnrealSharpEditorModule::OnAssetSearchRootAdded));

	UAssetManagerSettings* Settings = UAssetManagerSettings::StaticClass()->GetDefaultObject<UAssetManagerSettings>();
	Settings->OnSettingChanged().AddStatic(&FUnrealSharpEditorModule::OnAssetManagerSettingsChanged);
	
	ProcessAssetIds();
}

bool FUnrealSharpEditorModule::IsRegisteredAssetType(const FAssetData& AssetData)
{
	return IsRegisteredAssetType(AssetData.GetClass());
}

bool FUnrealSharpEditorModule::IsRegisteredAssetType(UClass* Class)
{
	if (!IsValid(Class))
	{
		return false;
	}
	
	UAssetManager& AssetManager = UAssetManager::Get();
	const UAssetManagerSettings& Settings = AssetManager.GetSettings();
	
	bool bIsPrimaryAsset = false;
	for (const FPrimaryAssetTypeInfo& PrimaryAssetType : Settings.PrimaryAssetTypesToScan)
	{
		if (Class->IsChildOf(PrimaryAssetType.GetAssetBaseClass().Get()))
		{
			bIsPrimaryAsset = true;
			break;
		}
	}
	return bIsPrimaryAsset;
}

void FUnrealSharpEditorModule::OnAssetRemoved(const FAssetData& AssetData)
{
	if (!IsRegisteredAssetType(AssetData))
	{
		return;
	}
	WaitUpdateAssetTypes();
}

void FUnrealSharpEditorModule::OnAssetRenamed(const FAssetData& AssetData, const FString& OldObjectPath)
{
	if (!IsRegisteredAssetType(AssetData))
	{
		return;
	}
	WaitUpdateAssetTypes();
}

void FUnrealSharpEditorModule::OnInMemoryAssetCreated(UObject* Object)
{
	if (!IsRegisteredAssetType(Object))
	{
		return;
	}
	WaitUpdateAssetTypes();
}

void FUnrealSharpEditorModule::OnInMemoryAssetDeleted(UObject* Object)
{
	if (!IsRegisteredAssetType(Object))
	{
		return;
	}
	WaitUpdateAssetTypes();
}

void FUnrealSharpEditorModule::OnAssetManagerSettingsChanged(UObject* Object, FPropertyChangedEvent& PropertyChangedEvent)
{
	WaitUpdateAssetTypes();
	GEditor->GetTimerManager()->SetTimerForNextTick(FTimerDelegate::CreateStatic(&FUnrealSharpEditorModule::ProcessAssetTypes));
}

void FUnrealSharpEditorModule::WaitUpdateAssetTypes()
{
	GEditor->GetTimerManager()->SetTimerForNextTick(FTimerDelegate::CreateStatic(&FUnrealSharpEditorModule::ProcessAssetIds));
}

void FUnrealSharpEditorModule::OnAssetSearchRootAdded(const FString& RootPath)
{
	WaitUpdateAssetTypes();
}

void FUnrealSharpEditorModule::ProcessGameplayTags()
{
	TArray<const FGameplayTagSource*> Sources;
	UGameplayTagsManager& Manager = UGameplayTagsManager::Get();

	const int32 NumValues = StaticEnum<EGameplayTagSourceType>()->NumEnums();
	for (int32 Index = 0; Index < NumValues; Index++)
	{
		EGameplayTagSourceType SourceType = static_cast<EGameplayTagSourceType>(Index);
		Manager.FindTagSourcesWithType(SourceType, Sources);
	}

	FCSScriptBuilder ScriptBuilder(FCSScriptBuilder::IndentType::Tabs);
	ScriptBuilder.AppendLine(TEXT("using UnrealSharp.GameplayTags;"));
	ScriptBuilder.AppendLine();
	ScriptBuilder.AppendLine(TEXT("public static class GameplayTags"));
	ScriptBuilder.OpenBrace();
		
	TArray<FName> TagNames;
	auto GenerateGameplayTag = [&ScriptBuilder, &TagNames](const FGameplayTagTableRow& RowTag)
	{
		if (TagNames.Contains(RowTag.Tag))
		{
			return;
		}
		
		const FString TagName = RowTag.Tag.ToString();
		const FString TagNameVariable = TagName.Replace(TEXT("."), TEXT("_"));
		ScriptBuilder.AppendLine(FString::Printf(TEXT("public static readonly FGameplayTag %s = new(\"%s\");"), *TagNameVariable, *TagName));
		TagNames.Add(RowTag.Tag);
	};

	for (const FGameplayTagSource* Source : Sources)
	{
		if (Source->SourceTagList)
		{
			for (const FGameplayTagTableRow& RowTag : Source->SourceTagList->GameplayTagList)
			{
				GenerateGameplayTag(RowTag);
			}
		}

		if (Source->SourceRestrictedTagList)
		{
			for (const FGameplayTagTableRow& RowTag : Source->SourceRestrictedTagList->RestrictedGameplayTagList)
			{
				GenerateGameplayTag(RowTag);
			}
		}
	}

	ScriptBuilder.CloseBrace();
	SaveRuntimeGlue(ScriptBuilder, TEXT("GameplayTags"));
}

FString ReplaceSpecialCharacters(const FString& Input)
{
	FString ModifiedString = Input;
	FRegexPattern Pattern(TEXT("[^a-zA-Z0-9_]"));
	FRegexMatcher Matcher(Pattern, ModifiedString);
	
	while (Matcher.FindNext())
	{
		int32 MatchStart = Matcher.GetMatchBeginning();
		int32 MatchEnd = Matcher.GetMatchEnding();
		ModifiedString = ModifiedString.Mid(0, MatchStart) + TEXT("_") + ModifiedString.Mid(MatchEnd);
		Matcher = FRegexMatcher(Pattern, ModifiedString);
	}

	return ModifiedString;
}

void FUnrealSharpEditorModule::ProcessAssetIds()
{
	UAssetManager& AssetManager = UAssetManager::Get();
	const UAssetManagerSettings& Settings = AssetManager.GetSettings();

	FCSScriptBuilder ScriptBuilder(FCSScriptBuilder::IndentType::Tabs);
	ScriptBuilder.AppendLine();
	ScriptBuilder.AppendLine(TEXT("using UnrealSharp.CoreUObject;"));
	ScriptBuilder.AppendLine();
	ScriptBuilder.AppendLine(TEXT("public static class AssetIds"));
	ScriptBuilder.OpenBrace();
	
	for (const FPrimaryAssetTypeInfo& PrimaryAssetType : Settings.PrimaryAssetTypesToScan)
	{
		TArray<FPrimaryAssetId> PrimaryAssetIdList;
		AssetManager.GetPrimaryAssetIdList(PrimaryAssetType.PrimaryAssetType, PrimaryAssetIdList);
		for (const FPrimaryAssetId& AssetType : PrimaryAssetIdList)
		{
			FString AssetName = PrimaryAssetType.PrimaryAssetType.ToString() + TEXT(".") + AssetType.PrimaryAssetName.ToString();
			AssetName = ReplaceSpecialCharacters(AssetName);
			
			ScriptBuilder.AppendLine(FString::Printf(TEXT("public static readonly FPrimaryAssetId %s = new(\"%s\", \"%s\");"),
				*AssetName, *AssetType.PrimaryAssetType.GetName().ToString(), *AssetType.PrimaryAssetName.ToString()));
		}
	}

	ScriptBuilder.CloseBrace();
	SaveRuntimeGlue(ScriptBuilder, TEXT("AssetIds"));
}

void FUnrealSharpEditorModule::ProcessAssetTypes()
{
	UAssetManager& AssetManager = UAssetManager::Get();
	const UAssetManagerSettings& Settings = AssetManager.GetSettings();

	FCSScriptBuilder ScriptBuilder(FCSScriptBuilder::IndentType::Tabs);
	ScriptBuilder.AppendLine();
	ScriptBuilder.AppendLine(TEXT("using UnrealSharp.CoreUObject;"));
	ScriptBuilder.AppendLine();
	ScriptBuilder.AppendLine(TEXT("public static class AssetTypes"));
	ScriptBuilder.OpenBrace();
	
	for (const FPrimaryAssetTypeInfo& PrimaryAssetType : Settings.PrimaryAssetTypesToScan)
	{
		FString AssetTypeName = ReplaceSpecialCharacters(PrimaryAssetType.PrimaryAssetType.ToString());

		ScriptBuilder.AppendLine(FString::Printf(TEXT("public static readonly FPrimaryAssetType %s = new(\"%s\");"),
			*AssetTypeName, *PrimaryAssetType.PrimaryAssetType.ToString()));
	}

	ScriptBuilder.CloseBrace();
	SaveRuntimeGlue(ScriptBuilder, TEXT("AssetTypes"));
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
