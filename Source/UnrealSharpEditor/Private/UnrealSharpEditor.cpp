#include "UnrealSharpEditor.h"
#include "AssetToolsModule.h"
#include "CSBuildActionUtilities.h"
#include "CSBuildUtilties.h"
#include "CSEditorCommands.h"
#include "CSInstallationUtilities.h"
#include "CSStyle.h"
#include "DesktopPlatformModule.h"
#include "IPluginBrowser.h"
#include "ISettingsModule.h"
#include "LevelEditor.h"
#include "SourceCodeNavigation.h"
#include "SubobjectDataSubsystem.h"
#include "AssetActions/CSAssetTypeAction_CSBlueprint.h"
#include "Features/IPluginsEditorFeature.h"
#include "CSManager.h"
#include "Interfaces/IMainFrameModule.h"
#include "Interfaces/IPluginManager.h"
#include "Logging/StructuredLog.h"
#include "Misc/LowLevelTestAdapter.h"
#include "Misc/ScopedSlowTask.h"
#include "Plugins/CSPluginTemplateDescription.h"
#include "Slate/CSNewProjectWizard.h"
#include "CSPathsUtilities.h"
#include "CSProjectUtilities.h"
#include "CSUnrealSharpEditorSettings.h"
#include "HotReload/CSHotReloadSubsystem.h"
#include "Containers/Set.h"
#include "Settings/PlatformsMenuSettings.h"
#include "Slate/SCSTypeWizard.h"

#define LOCTEXT_NAMESPACE "FUnrealSharpEditorModule"

DEFINE_LOG_CATEGORY(LogUnrealSharpEditor);

FUnrealSharpEditorModule& FUnrealSharpEditorModule::Get()
{
	return FModuleManager::LoadModuleChecked<FUnrealSharpEditorModule>("UnrealSharpEditor");
}

void FUnrealSharpEditorModule::StartupModule()
{
	IAssetTools& AssetTools = FModuleManager::LoadModuleChecked<FAssetToolsModule>("AssetTools").Get();
	AssetTools.RegisterAssetTypeActions(MakeShared<FCSAssetTypeAction_CSBlueprint>());

	TArray<FString> ProjectPaths;
	UnrealSharp::Project::GetAllProjectPaths(ProjectPaths);
	
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
		
		for (const UPackage* Package : UCSManager::Get().GetManagedPackages())
		{
			AssetToolsRef.GetWritableFolderPermissionList()->AddDenyListItem(Package->GetFName(), Package->GetFName());
		}
	}

	FCSStyle::Initialize();

	RegisterCommands();
	RegisterToolbar();
    RegisterPluginTemplates();
	
	UCSManager::Get().AddOrExecuteOnManagerInitialized(FCSManagerInitializedEvent::FDelegate::CreateLambda([this](UCSManager& Manager)
	{
		Manager.LoadPluginAssemblyByName("UnrealSharp.Editor");
	}));
}

void FUnrealSharpEditorModule::ShutdownModule()
{
	UToolMenus::UnRegisterStartupCallback(this);
	UToolMenus::UnregisterOwner(this);
    UnregisterPluginTemplates();
}

void FUnrealSharpEditorModule::InitializeManagedEditorCallbacks(FCSManagedEditorCallbacks Callbacks)
{
	ManagedUnrealSharpEditorCallbacks = Callbacks;
}

void FUnrealSharpEditorModule::OnCreateNewProject()
{
	OpenNewProjectDialog();
}

void FUnrealSharpEditorModule::OnCompileManagedCode()
{
	UCSHotReloadSubsystem::Get()->PerformHotReload();
}

void FUnrealSharpEditorModule::OnCreateNewClass()
{
	SCSTypeWizard::OpenDialog(SCSTypeWizard::FOnClassCreated::CreateRaw(this, &FUnrealSharpEditorModule::HandleNewClassCreated));
}

void FUnrealSharpEditorModule::HandleNewClassCreated(const FString& ClassName, const FString& FilePath)
{
	OpenSolution();
}

void FUnrealSharpEditorModule::OnRegenerateSolution()
{
	TMap<FString, FString> ActionArgs;
	ActionArgs.Add(TEXT("ForceGenerate"), TEXT("true"));
	
	if (!UnrealSharp::Build::InvokeUnrealSharpAutomation(UnrealSharp::BuildAction::GenerateUserSolution, &ActionArgs))
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
	if (!UnrealSharp::Build::InvokeUnrealSharpAutomation(UnrealSharp::BuildAction::MergeSolution))
	{
		return;
	}
}

void FUnrealSharpEditorModule::OnOpenSettings()
{
	const UDeveloperSettings* Settings = GetDefault<UCSUnrealSharpEditorSettings>();
	ISettingsModule* SettingsModule = FModuleManager::GetModulePtr<ISettingsModule>("Settings");
	SettingsModule->ShowViewer(Settings->GetContainerName(), Settings->GetCategoryName(), Settings->GetSectionName());
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

	FString ExecutablePath = ArchiveDirectory / FApp::GetProjectName() + TEXT(".exe");
	if (!FPaths::FileExists(ExecutablePath))
	{
		FString DialogText = FString::Printf(TEXT("The executable for project '%s' could not be found in the directory: %s. Please select the root directory where you packaged your game."), FApp::GetProjectName(), *ArchiveDirectory);
		FMessageDialog::Open(EAppMsgType::Ok, FText::FromString(DialogText));
		return;
	}
	
	const UProjectPackagingSettings* PlatformsPackagingSettings = GetDefault<UProjectPackagingSettings>();
	
	TMap<FString, FString> Arguments;
	Arguments.Add(TEXT("ArchiveDirectory"), UnrealSharp::Paths::MakeQuotedPath(FPaths::Combine(ArchiveDirectory, FApp::GetProjectName())));
	
	int32 BuildConfigValue = static_cast<int32>(PlatformsPackagingSettings->BuildConfiguration);
	UProjectPackagingSettings::FConfigurationInfo ConfigurationInfo = UProjectPackagingSettings::ConfigurationInfo[BuildConfigValue];
	Arguments.Add(TEXT("UEBuildConfig"), ConfigurationInfo.Name.ToString());
	Arguments.Add(TEXT("UETargetType"), TEXT("Game"));
	
	FText BuildActionDisplayName = FText::Format(LOCTEXT("PackagingInProgress", "Packaging C# Project '{0}'"), FText::FromString(FApp::GetProjectName()));
	UnrealSharp::Build::InvokeUnrealSharpAutomation_Async(UnrealSharp::BuildAction::PackageProject, BuildActionDisplayName, &Arguments);
}

void FUnrealSharpEditorModule::OpenSolution()
{
	FString SolutionPath = FPaths::ConvertRelativePathToFull(UnrealSharp::Paths::GetPathToManagedSolution());

	if (!FPaths::FileExists(SolutionPath))
	{
		OnRegenerateSolution();
	}

	FString ExceptionMessage;
	if (ManagedUnrealSharpEditorCallbacks.OpenSolution(*SolutionPath, &ExceptionMessage))
	{
		return;
	}
	
	FMessageDialog::Open(EAppMsgType::Ok, FText::FromString(ExceptionMessage), FText::FromString(TEXT("Opening C# Project Failed")));
};

FString FUnrealSharpEditorModule::SelectArchiveDirectory()
{
	FString DestinationFolder;
	const void* ParentWindowHandle = FSlateApplication::Get().FindBestParentWindowHandleForDialogs(nullptr);
	
	const FString Title = FString::Printf(TEXT("Select the root directory for %s"), FApp::GetProjectName());
	if (!FDesktopPlatformModule::Get()->OpenDirectoryDialog(ParentWindowHandle, Title, FString(), DestinationFolder))
	{
		return FString();
	}

	return FPaths::ConvertRelativePathToFull(DestinationFolder);
}

TSharedRef<SWidget> FUnrealSharpEditorModule::GenerateUnrealSharpToolbar() const
{
    const FCSEditorCommands& CSCommands = FCSEditorCommands::Get();
    FMenuBuilder MenuBuilder(true, UnrealSharpCommands);

    if (UnrealSharp::InstallationUtilities::IsDotNetSdkInstalled())
    {
    	AppendBuildMenu(CSCommands, MenuBuilder);
    	AppendCodeMenu(CSCommands, MenuBuilder);
    	AppendProjectMenu(CSCommands, MenuBuilder);
    	AppendPackageMenu(CSCommands, MenuBuilder);
    }
	
	AppendPluginMenu(CSCommands, MenuBuilder);

    OnBuildingToolbar.Broadcast(MenuBuilder);

    return MenuBuilder.MakeWidget();
}

void FUnrealSharpEditorModule::AppendProjectMenu(const FCSEditorCommands& CSCommands, FMenuBuilder& MenuBuilder)
{
	MenuBuilder.BeginSection("Project", LOCTEXT("Project", "Project"));

	MenuBuilder.AddMenuEntry(CSCommands.CreateNewProject, NAME_None, TAttribute<FText>(), TAttribute<FText>(),
							 FSlateIcon(FAppStyle::Get().GetStyleSetName(), "Icons.Plus"));

	MenuBuilder.AddMenuEntry(CSCommands.OpenSolution, NAME_None, TAttribute<FText>(), TAttribute<FText>(),
							 FSourceCodeNavigation::GetOpenSourceCodeIDEIcon());

	MenuBuilder.AddMenuEntry(CSCommands.RegenerateSolution, NAME_None, TAttribute<FText>(), TAttribute<FText>(),
							 FSourceCodeNavigation::GetOpenSourceCodeIDEIcon());

	MenuBuilder.AddMenuEntry(CSCommands.MergeManagedSlnAndNativeSln, NAME_None, TAttribute<FText>(), TAttribute<FText>(),
							 FSourceCodeNavigation::GetOpenSourceCodeIDEIcon());

	MenuBuilder.EndSection();
}

void FUnrealSharpEditorModule::AppendPackageMenu(const FCSEditorCommands& CSCommands, FMenuBuilder& MenuBuilder)
{
	MenuBuilder.BeginSection("Package", LOCTEXT("Package", "Package"));

	MenuBuilder.AddMenuEntry(CSCommands.PackageProject, NAME_None, TAttribute<FText>(), TAttribute<FText>(),
							 FSlateIcon(FAppStyle::Get().GetStyleSetName(), "LevelEditor.Recompile"));

	MenuBuilder.EndSection();
}

void FUnrealSharpEditorModule::AppendCodeMenu(const FCSEditorCommands& CSCommands, FMenuBuilder& MenuBuilder)
{
	MenuBuilder.BeginSection("Code", LOCTEXT("Code", "Code"));

	MenuBuilder.AddMenuEntry(CSCommands.CreateNewClass, NAME_None, TAttribute<FText>(), TAttribute<FText>(),
							 FSlateIcon(FAppStyle::Get().GetStyleSetName(), "Icons.Plus"));

	MenuBuilder.EndSection();
}

void FUnrealSharpEditorModule::AppendBuildMenu(const FCSEditorCommands& CSCommands, FMenuBuilder& MenuBuilder)
{
	MenuBuilder.BeginSection("Build", LOCTEXT("Build", "Build"));

	MenuBuilder.AddMenuEntry(CSCommands.HotReload, NAME_None, TAttribute<FText>(), TAttribute<FText>(),
							 FSlateIcon(FAppStyle::Get().GetStyleSetName(), "LevelEditor.Recompile"));

	MenuBuilder.EndSection();
}

void FUnrealSharpEditorModule::AppendPluginMenu(const FCSEditorCommands& CSCommands, FMenuBuilder& MenuBuilder)
{
	MenuBuilder.BeginSection("Plugin", LOCTEXT("Plugin", "Plugin"));

	MenuBuilder.AddMenuEntry(CSCommands.OpenSettings, NAME_None, TAttribute<FText>(), TAttribute<FText>(),
							 FSlateIcon(FAppStyle::Get().GetStyleSetName(), "EditorPreferences.TabIcon"));

	MenuBuilder.AddMenuEntry(CSCommands.OpenDocumentation, NAME_None, TAttribute<FText>(), TAttribute<FText>(),
							 FSlateIcon(FAppStyle::Get().GetStyleSetName(), "MainFrame.DocumentationHome"));

	MenuBuilder.AddMenuEntry(CSCommands.ReportBug, NAME_None, TAttribute<FText>(), TAttribute<FText>(),
							 FSlateIcon(FAppStyle::Get().GetStyleSetName(), "MainFrame.ReportABug"));

	MenuBuilder.EndSection();
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
	FCSEditorCommands::Register();
	UnrealSharpCommands = MakeShareable(new FUICommandList);
	const FCSEditorCommands& EditorCommands = FCSEditorCommands::Get();
	
	UnrealSharpCommands->MapAction(EditorCommands.OpenSettings, FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnOpenSettings));
	UnrealSharpCommands->MapAction(EditorCommands.OpenDocumentation, FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnOpenDocumentation));
	UnrealSharpCommands->MapAction(EditorCommands.ReportBug, FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnReportBug));

	if (UnrealSharp::InstallationUtilities::IsDotNetSdkInstalled())
	{
		UnrealSharpCommands->MapAction(EditorCommands.HotReload, FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnCompileManagedCode));
		UnrealSharpCommands->MapAction(EditorCommands.CreateNewProject, FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnCreateNewProject));
		UnrealSharpCommands->MapAction(EditorCommands.CreateNewClass, FExecuteAction::CreateRaw(this, &FUnrealSharpEditorModule::OnCreateNewClass));
		UnrealSharpCommands->MapAction(EditorCommands.RegenerateSolution, FExecuteAction::CreateRaw(this, &FUnrealSharpEditorModule::OnRegenerateSolution));
		UnrealSharpCommands->MapAction(EditorCommands.OpenSolution, FExecuteAction::CreateRaw(this, &FUnrealSharpEditorModule::OnOpenSolution));
		UnrealSharpCommands->MapAction(EditorCommands.MergeManagedSlnAndNativeSln, FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnMergeManagedSlnAndNativeSln));
		UnrealSharpCommands->MapAction(EditorCommands.PackageProject, FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnPackageProject));
	}

	const FLevelEditorModule& LevelEditorModule = FModuleManager::GetModuleChecked<FLevelEditorModule>("LevelEditor");
	const TSharedRef<FUICommandList> Commands = LevelEditorModule.GetGlobalLevelEditorActions();
	Commands->Append(UnrealSharpCommands.ToSharedRef());
}

void FUnrealSharpEditorModule::RegisterToolbar()
{
	UToolMenu* ToolbarMenu = UToolMenus::Get()->ExtendMenu("LevelEditor.LevelEditorToolBar.PlayToolBar");
	FToolMenuSection& Section = ToolbarMenu->FindOrAddSection("PluginTools");

	FToolMenuEntry Entry = FToolMenuEntry::InitComboButton(
		"UnrealSharp",
		FUIAction(),
		FOnGetContent::CreateLambda([this]() { return GenerateUnrealSharpToolbar(); }),
		LOCTEXT("UnrealSharp_Label", "UnrealSharp"),
		LOCTEXT("UnrealSharp_Tooltip", "List of all UnrealSharp actions"),
		TAttribute<FSlateIcon>::CreateLambda([this]()
		{
			if (UCSHotReloadSubsystem* HotReloadSubsystem = UCSHotReloadSubsystem::Get())
			{
				if (HotReloadSubsystem->HasPendingHotReloadChanges())
				{
					return UnrealSharp::Icons::GetUnrealSharpIcon_Modified();
				}
			}
			
			return UnrealSharp::Icons::GetUnrealSharpIcon();
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
        PluginBaseDir / TEXT("Templates") / TEXT("Blank"), true, EHostType::Runtime, ELoadingPhase::Default);
	
    const TSharedRef<FPluginTemplateDescription> CSharpOnlyTemplate = MakeShared<FCSPluginTemplateDescription>(CSharpOnlyTemplateName, CSharpOnlyDescription,
        PluginBaseDir / TEXT("Templates") / TEXT("CSharpOnly"), true, EHostType::Runtime, ELoadingPhase::Default);

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
	UnrealSharp::Build::BuildUserSolution();
	UCSManager::Get().LoadUserAssemblyByName(*ModuleName, true);
	UCSHotReloadSubsystem::Get()->PauseHotReload(TEXT("Loading new C# project"));
	ManagedUnrealSharpEditorCallbacks.LoadProject(*ModulePath, (void*)&FUnrealSharpEditorModule::OnProjectLoaded);
}

void FUnrealSharpEditorModule::OnProjectLoaded()
{
	AsyncTask(ENamedThreads::GameThread, []()
	{
		UCSHotReloadSubsystem::Get()->ResumeHotReload();
		UCSHotReloadSubsystem::Get()->RefreshDirectoryWatchers();
	});
}

void FUnrealSharpEditorModule::AddNewProject(const FString& ModuleName, const FString& ProjectParentFolder, const FString& ProjectRoot, TMap<FString, FString> ActionArgs, bool bOpenProject)
{
	FString ProjectFolder = FPaths::Combine(ProjectParentFolder, ModuleName);
	FString CsProjPath = FPaths::Combine(ProjectFolder, ModuleName + ".csproj");
	
	if (FPaths::FileExists(CsProjPath))
	{
		return;
	}
	
	ActionArgs.Add(TEXT("ProjectName"), ModuleName);
	ActionArgs.Add(TEXT("ProjectFolder"), UnrealSharp::Paths::MakeQuotedPath(FPaths::ConvertRelativePathToFull(ProjectFolder)));
	ActionArgs.Add(TEXT("GenerateSolution"), TEXT("true"));
	ActionArgs.Add(TEXT("RunUSharpProjectSetup"), TEXT("true"));
	
	IUATHelperModule::UatTaskResultCallack UATCallback = [this, ModuleName, CsProjPath, bOpenProject](FString ReturnCode, double)
	{
		if (ReturnCode != TEXT("Completed"))
		{
			return;
		}
		
		AsyncTask(ENamedThreads::GameThread, [this, ModuleName, CsProjPath, bOpenProject]()
		{
			if (!bOpenProject)
			{
				return;
			}
	
			LoadNewProject(ModuleName, CsProjPath);
			OpenSolution();
		});
	};
	
	FText BuildActionDisplayName = FText::Format(LOCTEXT("GeneratingProject", "Generating C# Project '{0}'"), FText::FromString(ModuleName));
	UnrealSharp::Build::InvokeUnrealSharpAutomation_Async(UnrealSharp::BuildAction::GenerateProject, BuildActionDisplayName, &ActionArgs, UATCallback);
}

#undef LOCTEXT_NAMESPACE

IMPLEMENT_MODULE(FUnrealSharpEditorModule, UnrealSharpEditor)
