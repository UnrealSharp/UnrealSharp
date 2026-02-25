#include "UnrealSharpEditor.h"
#include "AssetToolsModule.h"
#include "CSEditorCommands.h"
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
#include "Framework/Notifications/NotificationManager.h"
#include "Interfaces/IMainFrameModule.h"
#include "Interfaces/IPluginManager.h"
#include "Logging/StructuredLog.h"
#include "Misc/LowLevelTestAdapter.h"
#include "Misc/ScopedSlowTask.h"
#include "Plugins/CSPluginTemplateDescription.h"
#include "Slate/CSNewProjectWizard.h"
#include "CSProcUtilities.h"
#include "CSUnrealSharpEditorSettings.h"
#include "Widgets/Notifications/SNotificationList.h"
#include "UnrealSharpUtils.h"
#include "HotReload/CSHotReloadSubsystem.h"
#include "Containers/Set.h"

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
	UCSProcUtilities::GetAllProjectPaths(ProjectPaths);
	
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

		UCSManager::Get().ForEachManagedPackage([&AssetToolsRef](const UPackage* Package)
		{
			AssetToolsRef.GetWritableFolderPermissionList()->AddDenyListItem(Package->GetFName(), Package->GetFName());
		});
	}

	FCSStyle::Initialize();

	RegisterCommands();
	RegisterToolbar();
    RegisterPluginTemplates();
	
	UCSManager::Get().AddOrExecuteOnManagerInitialized(UCSManager::FCSManagerInitializedEvent::FDelegate::CreateLambda([this](UCSManager& Manager)
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

void FUnrealSharpEditorModule::OnRegenerateSolution()
{
	if (!UCSProcUtilities::InvokeUnrealSharpBuildTool(BUILD_ACTION_GENERATE_SOLUTION))
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
	static FString ManagedSolutionPath = FPaths::ConvertRelativePathToFull(UCSProcUtilities::GetPathToManagedSolution());

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

	TArray<FString> ManagedSlnFileLines;
	FFileHelper::LoadFileToStringArray(ManagedSlnFileLines, *ManagedSolutionPath);

	auto Trimmed = [](const FString& InLine)
	{
		return InLine.TrimStartAndEnd();
	};

	auto FindSectionRange = [&Trimmed](const TArray<FString>& Lines, const FString& SectionName, int32& OutStart, int32& OutEnd)
	{
		OutStart = INDEX_NONE;
		OutEnd = INDEX_NONE;

		const FString SectionPrefix = FString::Printf(TEXT("GlobalSection(%s)"), *SectionName);
		for (int32 idx = 0; idx < Lines.Num(); ++idx)
		{
			const FString Line = Trimmed(Lines[idx]);
			if (!Line.StartsWith(SectionPrefix))
			{
				continue;
			}

			OutStart = idx;
			for (int32 sectionIdx = idx + 1; sectionIdx < Lines.Num(); ++sectionIdx)
			{
				if (Trimmed(Lines[sectionIdx]) == TEXT("EndGlobalSection"))
				{
					OutEnd = sectionIdx;
					return true;
				}
			}

			return false;
		}

		return false;
	};

	auto ExtractQuotedParts = [](const FString& Line, TArray<FString>& OutParts)
	{
		OutParts.Reset();
		bool bInQuote = false;
		int32 StartIdx = INDEX_NONE;

		for (int32 idx = 0; idx < Line.Len(); ++idx)
		{
			if (Line[idx] != TEXT('"'))
			{
				continue;
			}

			if (!bInQuote)
			{
				bInQuote = true;
				StartIdx = idx + 1;
			}
			else
			{
				OutParts.Add(Line.Mid(StartIdx, idx - StartIdx));
				bInQuote = false;
				StartIdx = INDEX_NONE;
			}
		}

		return OutParts.Num() >= 4;
	};

	auto ToWindowsSlashes = [](FString InPath)
	{
		InPath.ReplaceInline(TEXT("/"), TEXT("\\"));
		return InPath;
	};

	struct FSlnProjectBlock
	{
		TArray<FString> Lines;
		FString ProjectGuid;
		bool bIsCSharpProject = false;
	};

	TArray<FSlnProjectBlock> ManagedProjectBlocks;
	TArray<FString> ManagedNestedMappings;
	TSet<FString> ManagedCSharpProjectGuids;

	const FString ManagedSolutionDirectory = FPaths::GetPath(ManagedSolutionPath);
	const FString ProjectRootDirectory = FPaths::ConvertRelativePathToFull(FPaths::ProjectDir());

	for (int32 idx = 0; idx < ManagedSlnFileLines.Num(); ++idx)
	{
		const FString Line = Trimmed(ManagedSlnFileLines[idx]);
		if (!Line.StartsWith(TEXT("Project(")))
		{
			continue;
		}

		int32 EndProjectIdx = INDEX_NONE;
		for (int32 searchIdx = idx + 1; searchIdx < ManagedSlnFileLines.Num(); ++searchIdx)
		{
			if (Trimmed(ManagedSlnFileLines[searchIdx]) == TEXT("EndProject"))
			{
				EndProjectIdx = searchIdx;
				break;
			}
		}

		if (EndProjectIdx == INDEX_NONE)
		{
			continue;
		}

		FSlnProjectBlock Block;
		for (int32 blockIdx = idx; blockIdx <= EndProjectIdx; ++blockIdx)
		{
			Block.Lines.Add(ManagedSlnFileLines[blockIdx]);
		}

		TArray<FString> QuotedParts;
		if (ExtractQuotedParts(Block.Lines[0], QuotedParts))
		{
			FString RelativeProjectPath = QuotedParts[2];
			const bool bContainsCsProj = RelativeProjectPath.EndsWith(TEXT(".csproj"), ESearchCase::IgnoreCase);

			if (bContainsCsProj)
			{
				FString AbsoluteProjectPath = FPaths::ConvertRelativePathToFull(ManagedSolutionDirectory / RelativeProjectPath);
				FString PathRelativeToProject = AbsoluteProjectPath;
				FPaths::MakePathRelativeTo(PathRelativeToProject, *ProjectRootDirectory);
				RelativeProjectPath = ToWindowsSlashes(PathRelativeToProject);
				Block.bIsCSharpProject = true;
			}

			Block.ProjectGuid = QuotedParts[3];
			QuotedParts[2] = RelativeProjectPath;
			Block.Lines[0] = FString::Printf(TEXT("Project(\"%s\") = \"%s\", \"%s\", \"%s\""),
				*QuotedParts[0], *QuotedParts[1], *QuotedParts[2], *QuotedParts[3]);

			if (Block.bIsCSharpProject)
			{
				ManagedCSharpProjectGuids.Add(Block.ProjectGuid);
			}
		}

		ManagedProjectBlocks.Add(MoveTemp(Block));
		idx = EndProjectIdx;
	}

	int32 ManagedNestedStartIdx = INDEX_NONE;
	int32 ManagedNestedEndIdx = INDEX_NONE;
	if (FindSectionRange(ManagedSlnFileLines, TEXT("NestedProjects"), ManagedNestedStartIdx, ManagedNestedEndIdx))
	{
		for (int32 idx = ManagedNestedStartIdx + 1; idx < ManagedNestedEndIdx; ++idx)
		{
			const FString Line = Trimmed(ManagedSlnFileLines[idx]);
			if (!Line.IsEmpty())
			{
				ManagedNestedMappings.Add(Line);
			}
		}
	}

	int32 LastEndProjectIdx = INDEX_NONE;
	for (int32 idx = 0; idx < NativeSlnFileLines.Num(); ++idx)
	{
		if (Trimmed(NativeSlnFileLines[idx]) == TEXT("EndProject"))
		{
			LastEndProjectIdx = idx;
		}
	}

	if (LastEndProjectIdx == INDEX_NONE)
	{
		FMessageDialog::Open(EAppMsgType::Ok, FText::FromString(TEXT("Failed to locate native solution project section.")));
		return;
	}

	int32 InsertProjectAt = LastEndProjectIdx + 1;
	for (const FSlnProjectBlock& Block : ManagedProjectBlocks)
	{
		for (const FString& BlockLine : Block.Lines)
		{
			NativeSlnFileLines.Insert(BlockLine, InsertProjectAt++);
		}
	}

	TArray<FString> NativeSolutionConfigs;
	int32 NativeSolutionConfigStartIdx = INDEX_NONE;
	int32 NativeSolutionConfigEndIdx = INDEX_NONE;
	if (FindSectionRange(NativeSlnFileLines, TEXT("SolutionConfigurationPlatforms"), NativeSolutionConfigStartIdx, NativeSolutionConfigEndIdx))
	{
		for (int32 idx = NativeSolutionConfigStartIdx + 1; idx < NativeSolutionConfigEndIdx; ++idx)
		{
			const FString Line = Trimmed(NativeSlnFileLines[idx]);
			if (Line.IsEmpty())
			{
				continue;
			}

			FString LeftSide;
			FString RightSide;
			if (Line.Split(TEXT("="), &LeftSide, &RightSide))
			{
				NativeSolutionConfigs.Add(LeftSide.TrimStartAndEnd());
			}
		}
	}

	int32 NativeProjectConfigStartIdx = INDEX_NONE;
	int32 NativeProjectConfigEndIdx = INDEX_NONE;
	if (FindSectionRange(NativeSlnFileLines, TEXT("ProjectConfigurationPlatforms"), NativeProjectConfigStartIdx, NativeProjectConfigEndIdx))
	{
		TSet<FString> ExistingProjectConfigLines;
		for (int32 idx = NativeProjectConfigStartIdx + 1; idx < NativeProjectConfigEndIdx; ++idx)
		{
			ExistingProjectConfigLines.Add(Trimmed(NativeSlnFileLines[idx]));
		}

		TArray<FString> ProjectConfigLinesToInsert;
		for (const FString& ManagedGuid : ManagedCSharpProjectGuids)
		{
			for (const FString& SolutionConfig : NativeSolutionConfigs)
			{
				const FString ActiveCfgLine = FString::Printf(TEXT("%s.%s.ActiveCfg = Development|Any CPU"), *ManagedGuid, *SolutionConfig);
				const FString BuildCfgLine = FString::Printf(TEXT("%s.%s.Build.0 = Development|Any CPU"), *ManagedGuid, *SolutionConfig);

				if (!ExistingProjectConfigLines.Contains(ActiveCfgLine))
				{
					ProjectConfigLinesToInsert.Add(FString::Printf(TEXT("\t\t%s"), *ActiveCfgLine));
					ExistingProjectConfigLines.Add(ActiveCfgLine);
				}

				if (!ExistingProjectConfigLines.Contains(BuildCfgLine))
				{
					ProjectConfigLinesToInsert.Add(FString::Printf(TEXT("\t\t%s"), *BuildCfgLine));
					ExistingProjectConfigLines.Add(BuildCfgLine);
				}
			}
		}

		if (!ProjectConfigLinesToInsert.IsEmpty())
		{
			int32 InsertConfigAt = NativeProjectConfigEndIdx;
			for (const FString& ConfigLine : ProjectConfigLinesToInsert)
			{
				NativeSlnFileLines.Insert(ConfigLine, InsertConfigAt++);
			}
		}
	}

	int32 NativeNestedStartIdx = INDEX_NONE;
	int32 NativeNestedEndIdx = INDEX_NONE;
	if (FindSectionRange(NativeSlnFileLines, TEXT("NestedProjects"), NativeNestedStartIdx, NativeNestedEndIdx))
	{
		TSet<FString> ExistingNestedMappings;
		for (int32 idx = NativeNestedStartIdx + 1; idx < NativeNestedEndIdx; ++idx)
		{
			ExistingNestedMappings.Add(Trimmed(NativeSlnFileLines[idx]));
		}

		TArray<FString> NestedLinesToInsert;
		for (const FString& ManagedNestedMapping : ManagedNestedMappings)
		{
			const FString TrimmedMapping = Trimmed(ManagedNestedMapping);
			if (!ExistingNestedMappings.Contains(TrimmedMapping))
			{
				NestedLinesToInsert.Add(FString::Printf(TEXT("\t\t%s"), *TrimmedMapping));
				ExistingNestedMappings.Add(TrimmedMapping);
			}
		}

		if (!NestedLinesToInsert.IsEmpty())
		{
			int32 InsertNestedAt = NativeNestedEndIdx;
			for (const FString& NestedLine : NestedLinesToInsert)
			{
				NativeSlnFileLines.Insert(NestedLine, InsertNestedAt++);
			}
		}
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
	Arguments.Add("UETargetType", "Game");
	UCSProcUtilities::InvokeUnrealSharpBuildTool(BUILD_ACTION_PACKAGE_PROJECT, Arguments);

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
	FString SolutionPath = FPaths::ConvertRelativePathToFull(UCSProcUtilities::GetPathToManagedSolution());

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

TSharedRef<SWidget> FUnrealSharpEditorModule::GenerateUnrealSharpToolbar() const
{
	const FCSEditorCommands& CSCommands = FCSEditorCommands::Get();
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
	
	OnBuildingToolbar.Broadcast(MenuBuilder);

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
	FCSEditorCommands::Register();
	UnrealSharpCommands = MakeShareable(new FUICommandList);
	UnrealSharpCommands->MapAction(FCSEditorCommands::Get().CreateNewProject,
	                               FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnCreateNewProject));
	UnrealSharpCommands->MapAction(FCSEditorCommands::Get().HotReload,
	                               FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnCompileManagedCode));
	UnrealSharpCommands->MapAction(FCSEditorCommands::Get().RegenerateSolution,
	                               FExecuteAction::CreateRaw(this, &FUnrealSharpEditorModule::OnRegenerateSolution));
	UnrealSharpCommands->MapAction(FCSEditorCommands::Get().OpenSolution,
	                               FExecuteAction::CreateRaw(this, &FUnrealSharpEditorModule::OnOpenSolution));
	UnrealSharpCommands->MapAction(FCSEditorCommands::Get().MergeManagedSlnAndNativeSln,
								   FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnMergeManagedSlnAndNativeSln));
	UnrealSharpCommands->MapAction(FCSEditorCommands::Get().PackageProject,
	                               FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnPackageProject));
	UnrealSharpCommands->MapAction(FCSEditorCommands::Get().OpenSettings,
	                               FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnOpenSettings));
	UnrealSharpCommands->MapAction(FCSEditorCommands::Get().OpenDocumentation,
	                               FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnOpenDocumentation));
	UnrealSharpCommands->MapAction(FCSEditorCommands::Get().ReportBug,
	                               FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnReportBug));

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

void FUnrealSharpEditorModule::LoadNewProject(const FString& ModulePath) const
{
	UCSProcUtilities::BuildUserSolution();
	UCSHotReloadSubsystem::Get()->PauseHotReload(TEXT("Loading new C# project"));
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

void FUnrealSharpEditorModule::AddNewProject(const FString& ModuleName, const FString& ProjectParentFolder, const FString& ProjectRoot, TMap<FString, FString> ExtraArguments, bool bOpenProject)
{
	FString ProjectFolder = FPaths::Combine(ProjectParentFolder, ModuleName);
	FString CsProjPath = FPaths::Combine(ProjectFolder, ModuleName + ".csproj");
	
	if (FPaths::FileExists(CsProjPath))
	{
		return;
	}
	
	ExtraArguments.Add(TEXT("NewProjectName"), ModuleName);
	ExtraArguments.Add(TEXT("NewProjectFolder"), FCSUnrealSharpUtils::MakeQuotedPath(FPaths::ConvertRelativePathToFull(ProjectParentFolder)));
	
	FString FullProjectRoot = FPaths::ConvertRelativePathToFull(ProjectRoot);
	ExtraArguments.Add(TEXT("ProjectRoot"), FCSUnrealSharpUtils::MakeQuotedPath(FullProjectRoot));

	if (!UCSProcUtilities::InvokeUnrealSharpBuildTool(BUILD_ACTION_GENERATE_PROJECT, ExtraArguments))
	{
		UE_LOGFMT(LogUnrealSharpEditor, Error, "Failed to generate project %s in %s", *ModuleName, *ProjectParentFolder);
		return;
	}
	
	if (!bOpenProject)
	{
		return;
	}
	
	LoadNewProject(CsProjPath);
	OpenSolution();
}

#undef LOCTEXT_NAMESPACE

IMPLEMENT_MODULE(FUnrealSharpEditorModule, UnrealSharpEditor)
