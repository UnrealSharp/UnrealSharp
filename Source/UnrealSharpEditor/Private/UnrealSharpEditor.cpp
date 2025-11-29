#include "UnrealSharpEditor.h"
#include "AssetToolsModule.h"
#include "CSUnrealSharpEditorCommands.h"
#include "CSStyle.h"
#include "DesktopPlatformModule.h"
#include "IPluginBrowser.h"
#include "ISettingsModule.h"
#include "LevelEditor.h"
#include "SourceCodeNavigation.h"
#include "SubobjectDataSubsystem.h"
#include "UnrealSharpRuntimeGlue.h"
#include "AssetActions/CSAssetTypeAction_CSBlueprint.h"
#include "Features/IPluginsEditorFeature.h"
#include "CSManager.h"
#include "Framework/Notifications/NotificationManager.h"
#include "Interfaces/IMainFrameModule.h"
#include "Interfaces/IPluginManager.h"
#include "Logging/StructuredLog.h"
#include "Misc/LowLevelTestAdapter.h"
#include "Misc/ScopedSlowTask.h"
#include "Plugins/CSPluginTemplateDescription.h"
#include "Slate/CSNewProjectWizard.h"
#include "CSProcHelper.h"
#include "CSUnrealSharpEditorSettings.h"
#include "Widgets/Notifications/SNotificationList.h"
#include "UnrealSharpUtils.h"
#include "HotReload/CSHotReloadSubsystem.h"

#define LOCTEXT_NAMESPACE "FUnrealSharpEditorModule"

DEFINE_LOG_CATEGORY(LogUnrealSharpEditor);

FUnrealSharpEditorModule& FUnrealSharpEditorModule::Get()
{
	return FModuleManager::LoadModuleChecked<FUnrealSharpEditorModule>("UnrealSharpEditor");
}

void FUnrealSharpEditorModule::StartupModule()
{
	Manager = &UCSManager::GetOrCreate();
	IAssetTools& AssetTools = FModuleManager::LoadModuleChecked<FAssetToolsModule>("AssetTools").Get();
	AssetTools.RegisterAssetTypeActions(MakeShared<FCSAssetTypeAction_CSBlueprint>());

	TArray<FString> ProjectPaths;
	FCSProcHelper::GetAllProjectPaths(ProjectPaths);
	
	if (ProjectPaths.IsEmpty())
	{
		IMainFrameModule::Get().OnMainFrameCreationFinished().AddLambda([this](TSharedPtr<SWindow>, bool)
		{
			SuggestProjectSetup();
		});
	}

	// Make managed types not available for edit in the editor
	{
		FAssetToolsModule& AssetToolsModule = FModuleManager::LoadModuleChecked<FAssetToolsModule>(TEXT("AssetTools"));
		IAssetTools& AssetToolsRef = AssetToolsModule.Get();

		Manager->ForEachManagedPackage([&AssetToolsRef](const UPackage* Package)
		{
			AssetToolsRef.GetWritableFolderPermissionList()->AddDenyListItem(Package->GetFName(), Package->GetFName());
		});
	}

	FCSStyle::Initialize();

	RegisterCommands();
	RegisterMenu();
    RegisterPluginTemplates();

	UCSManager& CSharpManager = UCSManager::Get();
	CSharpManager.LoadPluginAssemblyByName(TEXT("UnrealSharp.Editor"), false);
}

void FUnrealSharpEditorModule::ShutdownModule()
{
	UToolMenus::UnRegisterStartupCallback(this);
	UToolMenus::UnregisterOwner(this);
    UnregisterPluginTemplates();
}

void FUnrealSharpEditorModule::InitializeUnrealSharpEditorCallbacks(FCSManagedUnrealSharpEditorCallbacks Callbacks)
{
	ManagedUnrealSharpEditorCallbacks = Callbacks;
}

void FUnrealSharpEditorModule::OnCreateNewProject()
{
	OpenNewProjectDialog();
}

void FUnrealSharpEditorModule::OnCompileManagedCode()
{
	UCSHotReloadSubsystem::Get()->PerformHotReloadOnPendingChanges();
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

void FUnrealSharpEditorModule::OnMergeManagedSlnAndNativeSln()
{
	static FString NativeSolutionPath = FPaths::ProjectDir() / FApp::GetProjectName() + ".sln";
	static FString ManagedSolutionPath = FPaths::ConvertRelativePathToFull(FCSProcHelper::GetPathToManagedSolution());

	if (!FPaths::FileExists(NativeSolutionPath))
	{
		FString DialogText = FString::Printf(TEXT("Failed to load native solution %s"), *NativeSolutionPath);
		FMessageDialog::Open(EAppMsgType::Ok, FText::FromString(DialogText));
		return;
	}

	if (!FPaths::FileExists(ManagedSolutionPath))
	{
		FString DialogText = FString::Printf(TEXT("Failed to load managed solution %s"), *ManagedSolutionPath);
		FMessageDialog::Open(EAppMsgType::Ok, FText::FromString(DialogText));
		return;
	}

	TArray<FString> NativeSlnFileLines;
	FFileHelper::LoadFileToStringArray(NativeSlnFileLines, *NativeSolutionPath);

	int32 LastEndProjectIdx = 0;

	for (int32 idx = 0; idx < NativeSlnFileLines.Num(); ++idx)
	{
		FString Line = NativeSlnFileLines[idx];
		Line.ReplaceInline(TEXT("\n"), TEXT(""));
		if (Line == TEXT("EndProject"))
		{
			LastEndProjectIdx = idx;
		}
	}

	TArray<FString> ManagedSlnFileLines;
	FFileHelper::LoadFileToStringArray(ManagedSlnFileLines, *ManagedSolutionPath);

	TArray<FString> ManagedProjectLines;

	for (int32 idx = 0; idx < ManagedSlnFileLines.Num(); ++idx)
	{
		FString Line = ManagedSlnFileLines[idx];
		Line.ReplaceInline(TEXT("\n"), TEXT(""));
		if (Line.StartsWith(TEXT("Project(\"{")) || Line.StartsWith(TEXT("EndProject")))
		{
			ManagedProjectLines.Add(Line);
		}
	}

	for (int32 idx = 0; idx < ManagedProjectLines.Num(); ++idx)
	{
		FString Line = ManagedProjectLines[idx];
		if (Line.StartsWith(TEXT("Project(\"{")) && Line.Contains(TEXT(".csproj")))
		{
			TArray<FString> ProjectStrParts;
			Line.ParseIntoArray(ProjectStrParts, TEXT(", "));
			if(ProjectStrParts.Num() == 3 && ProjectStrParts[1].Contains(TEXT(".csproj")))
			{
				ProjectStrParts[1] = FString("\"Script\\") + ProjectStrParts[1].Mid(1);
				Line = FString::Join(ProjectStrParts, TEXT(", "));
			}
		}
		NativeSlnFileLines.Insert(Line, LastEndProjectIdx + 1 + idx);
	}

	FString MixedSlnPath = NativeSolutionPath.LeftChop(4) + FString(".Mixed.sln");
	FFileHelper::SaveStringArrayToFile(NativeSlnFileLines, *MixedSlnPath);
}

void FUnrealSharpEditorModule::OnOpenSettings()
{
	const UDeveloperSettings* Settings = GetDefault<UCSUnrealSharpEditorSettings>();
	FModuleManager::LoadModuleChecked<ISettingsModule>("Settings").ShowViewer(
		Settings->GetContainerName(), Settings->GetCategoryName(), Settings->GetSectionName());
}

void FUnrealSharpEditorModule::OnOpenDocumentation()
{
	FPlatformProcess::LaunchURL(TEXT("https://www.unrealsharp.com"), nullptr, nullptr);
}

void FUnrealSharpEditorModule::OnReportBug()
{
	FPlatformProcess::LaunchURL(TEXT("https://github.com/UnrealSharp/UnrealSharp/issues"), nullptr, nullptr);
}

void FUnrealSharpEditorModule::OnRefreshRuntimeGlue()
{
	FUnrealSharpRuntimeGlueModule& RuntimeGlueModule = FModuleManager::LoadModuleChecked<FUnrealSharpRuntimeGlueModule>(
		"UnrealSharpRuntimeGlue");
	RuntimeGlueModule.ForceRefreshRuntimeGlue();
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
		FString DialogText = FString::Printf(
			TEXT(
				"The executable for project '%s' could not be found in the directory: %s. Please select the root directory where you packaged your game."),
			FApp::GetProjectName(), *ArchiveDirectory);
		FMessageDialog::Open(EAppMsgType::Ok, FText::FromString(DialogText));
		return;
	}

	FScopedSlowTask Progress(1, LOCTEXT("USharpPackaging", "Packaging Project..."));
	Progress.MakeDialog();

	TMap<FString, FString> Arguments;
	Arguments.Add("ArchiveDirectory", FCSUnrealSharpUtils::MakeQuotedPath(ArchiveDirectory));
	Arguments.Add("BuildConfig", "Release");
	FCSProcHelper::InvokeUnrealSharpBuildTool(BUILD_ACTION_PACKAGE_PROJECT, Arguments);

	FNotificationInfo Info(
		FText::FromString(
			FString::Printf(TEXT("Project '%s' has been packaged successfully."), FApp::GetProjectName())));
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
	FString SolutionPath = FPaths::ConvertRelativePathToFull(FCSProcHelper::GetPathToManagedSolution());

	if (!FPaths::FileExists(SolutionPath))
	{
		OnRegenerateSolution();
	}

	FString ExceptionMessage;
	if (!ManagedUnrealSharpEditorCallbacks.OpenSolution(*SolutionPath, &ExceptionMessage))
	{
		FMessageDialog::Open(EAppMsgType::Ok, FText::FromString(ExceptionMessage), FText::FromString(TEXT("Opening C# Project Failed")));
		return;
	}
};

FString FUnrealSharpEditorModule::SelectArchiveDirectory()
{
	IDesktopPlatform* DesktopPlatform = FDesktopPlatformModule::Get();
	if (!DesktopPlatform)
	{
		return FString();
	}

	FString DestinationFolder;
	const void* ParentWindowHandle = FSlateApplication::Get().FindBestParentWindowHandleForDialogs(nullptr);
	const FString Title = LOCTEXT("USharpChooseArchiveRoot", "Find Archive Root").ToString();

	if (DesktopPlatform->OpenDirectoryDialog(ParentWindowHandle, Title, FString(), DestinationFolder))
	{
		return FPaths::ConvertRelativePathToFull(DestinationFolder);
	}

	return FString();
}

TSharedRef<SWidget> FUnrealSharpEditorModule::GenerateUnrealSharpMenu()
{
	const FCSUnrealSharpEditorCommands& CSCommands = FCSUnrealSharpEditorCommands::Get();
	FMenuBuilder MenuBuilder(true, UnrealSharpCommands);

	// Build
	MenuBuilder.BeginSection("Build", LOCTEXT("Build", "Build"));

	MenuBuilder.AddMenuEntry(CSCommands.HotReload, NAME_None, TAttribute<FText>(), TAttribute<FText>(),
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

	MenuBuilder.AddMenuEntry(CSCommands.MergeManagedSlnAndNativeSln, NAME_None, TAttribute<FText>(), TAttribute<FText>(),
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

	MenuBuilder.BeginSection("Glue", LOCTEXT("Glue", "Glue"));

	MenuBuilder.AddMenuEntry(CSCommands.RefreshRuntimeGlue, NAME_None, TAttribute<FText>(), TAttribute<FText>(),
	                         FSlateIcon(FAppStyle::GetAppStyleSetName(), "SourceControl.Actions.Refresh"));

	MenuBuilder.EndSection();

	return MenuBuilder.MakeWidget();
}

void FUnrealSharpEditorModule::OpenNewProjectDialog()
{
	TSharedRef<SWindow> AddCodeWindow = SNew(SWindow)
		.Title(LOCTEXT("CreateNewProject", "New C# Project"))
		.SizingRule(ESizingRule::Autosized)
		.SupportsMinimize(false);

	TSharedRef<SCSNewProjectDialog> NewProjectDialog = SNew(SCSNewProjectDialog);
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
	
	OpenNewProjectDialog();
}

void FUnrealSharpEditorModule::RegisterCommands()
{
	FCSUnrealSharpEditorCommands::Register();
	UnrealSharpCommands = MakeShareable(new FUICommandList);
	UnrealSharpCommands->MapAction(FCSUnrealSharpEditorCommands::Get().CreateNewProject,
	                               FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnCreateNewProject));
	UnrealSharpCommands->MapAction(FCSUnrealSharpEditorCommands::Get().HotReload,
	                               FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnCompileManagedCode));
	UnrealSharpCommands->MapAction(FCSUnrealSharpEditorCommands::Get().RegenerateSolution,
	                               FExecuteAction::CreateRaw(this, &FUnrealSharpEditorModule::OnRegenerateSolution));
	UnrealSharpCommands->MapAction(FCSUnrealSharpEditorCommands::Get().OpenSolution,
	                               FExecuteAction::CreateRaw(this, &FUnrealSharpEditorModule::OnOpenSolution));
	UnrealSharpCommands->MapAction(FCSUnrealSharpEditorCommands::Get().MergeManagedSlnAndNativeSln,
								   FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnMergeManagedSlnAndNativeSln));
	UnrealSharpCommands->MapAction(FCSUnrealSharpEditorCommands::Get().PackageProject,
	                               FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnPackageProject));
	UnrealSharpCommands->MapAction(FCSUnrealSharpEditorCommands::Get().OpenSettings,
	                               FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnOpenSettings));
	UnrealSharpCommands->MapAction(FCSUnrealSharpEditorCommands::Get().OpenDocumentation,
	                               FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnOpenDocumentation));
	UnrealSharpCommands->MapAction(FCSUnrealSharpEditorCommands::Get().ReportBug,
	                               FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnReportBug));
	UnrealSharpCommands->MapAction(FCSUnrealSharpEditorCommands::Get().RefreshRuntimeGlue,
							   FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnRefreshRuntimeGlue));

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
		FOnGetContent::CreateLambda([this]() { return GenerateUnrealSharpMenu(); }),
		LOCTEXT("UnrealSharp_Label", "UnrealSharp"),
		LOCTEXT("UnrealSharp_Tooltip", "List of all UnrealSharp actions"),
		TAttribute<FSlateIcon>::CreateLambda([this]()
		{
			return UCSHotReloadSubsystem::Get()->GetMenuIcon();
		}));

	Section.AddEntry(Entry);
}

void FUnrealSharpEditorModule::RegisterPluginTemplates()
{
    IPluginBrowser& PluginBrowser = IPluginBrowser::Get();
    const FString PluginBaseDir = FPaths::ConvertRelativePathToFull(IPluginManager::Get().FindPlugin(UE_PLUGIN_NAME)->GetBaseDir());

    const FText BlankTemplateName = LOCTEXT("UnrealSharp_BlankLabel", "C++/C# Joint");
	const FText CSharpOnlyTemplateName = LOCTEXT("UnrealSharp_CSharpOnlyLabel", "C# Only");

	const FText BlankDescription = LOCTEXT("UnrealSharp_BlankTemplateDesc", "Create a blank plugin with a minimal amount of C++ and C# code.");
	const FText CSharpOnlyDescription = LOCTEXT("UnrealSharp_CSharpOnlyTemplateDesc", "Create a blank plugin that can only contain content and C# scripts.");
	
    const TSharedRef<FPluginTemplateDescription> BlankTemplate = MakeShared<FCSPluginTemplateDescription>(BlankTemplateName, BlankDescription,
        PluginBaseDir / TEXT("Templates") / TEXT("Blank"), true, EHostType::Runtime, ELoadingPhase::Default, true);
	
    const TSharedRef<FPluginTemplateDescription> CSharpOnlyTemplate = MakeShared<FCSPluginTemplateDescription>(CSharpOnlyTemplateName, CSharpOnlyDescription,
        PluginBaseDir / TEXT("Templates") / TEXT("CSharpOnly"), true, EHostType::Runtime, ELoadingPhase::Default, false);

    PluginBrowser.RegisterPluginTemplate(BlankTemplate);
    PluginBrowser.RegisterPluginTemplate(CSharpOnlyTemplate);

    PluginTemplates.Add(BlankTemplate);
    PluginTemplates.Add(CSharpOnlyTemplate);
}

void FUnrealSharpEditorModule::UnregisterPluginTemplates()
{
    IPluginBrowser& PluginBrowser = IPluginBrowser::Get();
    for (const TSharedRef<FPluginTemplateDescription>& Template : PluginTemplates)
    {
        PluginBrowser.UnregisterPluginTemplate(Template);
    }
}

void FUnrealSharpEditorModule::LoadNewProject(const FString& ModuleName, const FString& ModulePath) const
{
	UCSHotReloadSubsystem::Get()->PauseHotReload(TEXT("Loading new C# project"));
	
	UCSManagedAssembly* LoadedAssembly = Manager->LoadUserAssemblyByName(*ModuleName);
	if (!LoadedAssembly || !LoadedAssembly->IsValidAssembly())
	{
		UE_LOGFMT(LogUnrealSharpEditor, Error, "Failed to load newly created project {ModuleName}", *ModuleName);
		UCSHotReloadSubsystem::Get()->ResumeHotReload();
		return;
	}
	
	ManagedUnrealSharpEditorCallbacks.LoadProject(*ModulePath, &FUnrealSharpEditorModule::OnProjectLoaded);
}

void FUnrealSharpEditorModule::OnProjectLoaded()
{
	AsyncTask(ENamedThreads::GameThread, []()
	{
		UCSHotReloadSubsystem::Get()->ResumeHotReload();
		UCSHotReloadSubsystem::Get()->RefreshDirectoryWatchers();
	});
}

void FUnrealSharpEditorModule::AddNewProject(const FString& ModuleName, const FString& ProjectParentFolder, const FString& ProjectRoot, const TMap<FString, FString>& ExtraArguments)
{
	TMap<FString, FString> Arguments = ExtraArguments;

	TMap<FString, FString> SolutionArguments;
	SolutionArguments.Add(TEXT("MODULENAME"), ModuleName);

	FString ProjectFolder = FPaths::Combine(ProjectParentFolder, ModuleName);
	FString ModuleFilePath = FPaths::Combine(ProjectFolder, ModuleName + ".cs");
	
	FillTemplateFile(TEXT("Module"), SolutionArguments, ModuleFilePath);

	Arguments.Add(TEXT("NewProjectName"), ModuleName);
	Arguments.Add(TEXT("NewProjectFolder"), FCSUnrealSharpUtils::MakeQuotedPath(FPaths::ConvertRelativePathToFull(ProjectParentFolder)));
	
	FString FullProjectRoot = FPaths::ConvertRelativePathToFull(ProjectRoot);
	Arguments.Add(TEXT("ProjectRoot"), FCSUnrealSharpUtils::MakeQuotedPath(FullProjectRoot));

	if (!FCSProcHelper::InvokeUnrealSharpBuildTool(BUILD_ACTION_GENERATE_PROJECT, Arguments))
	{
		UE_LOGFMT(LogUnrealSharpEditor, Error, "Failed to generate project %s in %s", *ModuleName, *ProjectParentFolder);
		return;
	}
	
	OpenSolution();
	
	FString ModulePath = FPaths::Combine(ProjectFolder, ModuleName + TEXT(".csproj"));
	LoadNewProject(ModuleName, ModulePath);
}

bool FUnrealSharpEditorModule::FillTemplateFile(const FString& TemplateName, TMap<FString, FString>& Replacements, const FString& Path)
{
	const FString FullFileName = FCSProcHelper::GetPluginDirectory() / TEXT("Templates") / TemplateName + TEXT(".cs.template");

	FString OutTemplate;
	if (!FFileHelper::LoadFileToString(OutTemplate, *FullFileName))
	{
		UE_LOG(LogUnrealSharpEditor, Error, TEXT("Failed to load template file %s"), *FullFileName);
		return false;
	}

	for (const TPair<FString, FString>& Replacement : Replacements)
	{
		FString ReplacementKey = TEXT("%") + Replacement.Key + TEXT("%");
		OutTemplate = OutTemplate.Replace(*ReplacementKey, *Replacement.Value);
	}

	if (!FFileHelper::SaveStringToFile(OutTemplate, *Path))
	{
		UE_LOG(LogUnrealSharpEditor, Error, TEXT("Failed to save %s when trying to create a template"), *Path);
		return false;
	}

	return true;
}

#undef LOCTEXT_NAMESPACE

IMPLEMENT_MODULE(FUnrealSharpEditorModule, UnrealSharpEditor)
