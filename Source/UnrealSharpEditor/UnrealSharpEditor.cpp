#include "UnrealSharpEditor.h"
#include "AssetToolsModule.h"
#include "BlueprintCompilationManager.h"
#include "BlueprintEditorLibrary.h"
#include "CSUnrealSharpEditorCommands.h"
#include "DirectoryWatcherModule.h"
#include "CSStyle.h"
#include "CSUnrealSharpEditorSettings.h"
#include "DesktopPlatformModule.h"
#include "IDirectoryWatcher.h"
#include "ISettingsModule.h"
#include "LevelEditor.h"
#include "SourceCodeNavigation.h"
#include "SubobjectDataSubsystem.h"
#include "UnrealSharpRuntimeGlue.h"
#include "AssetActions/CSAssetTypeAction_CSBlueprint.h"
#include "Engine/AssetManager.h"
#include "Engine/InheritableComponentHandler.h"
#include "UnrealSharpCore/CSManager.h"
#include "Framework/Notifications/NotificationManager.h"
#include "Interfaces/IMainFrameModule.h"
#include "Kismet2/BlueprintEditorUtils.h"
#include "Kismet2/DebuggerCommands.h"
#include "Logging/StructuredLog.h"
#include "Misc/ScopedSlowTask.h"
#include "Slate/CSNewProjectWizard.h"
#include "TypeGenerator/Register/CSGeneratedClassBuilder.h"
#include "UnrealSharpProcHelper/CSProcHelper.h"
#include "Widgets/Notifications/SNotificationList.h"
#include "TypeGenerator/CSClass.h"
#include "TypeGenerator/CSEnum.h"
#include "TypeGenerator/CSScriptStruct.h"
#include "UObject/UnrealTypePrivate.h"
#include "Utils/CSClassUtilities.h"

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

	FString FullScriptPath = FPaths::ConvertRelativePathToFull(FPaths::ProjectDir() / "Script");
	if (!FPaths::DirectoryExists(FullScriptPath))
	{
		FPlatformFileManager::Get().GetPlatformFile().CreateDirectory(*FullScriptPath);
	}

	FDirectoryWatcherModule& DirectoryWatcherModule = FModuleManager::LoadModuleChecked<FDirectoryWatcherModule>(
		"DirectoryWatcher");
	IDirectoryWatcher* DirectoryWatcher = DirectoryWatcherModule.Get();
	FDelegateHandle Handle;

	//Bind to directory watcher to look for changes in C# code.
	DirectoryWatcher->RegisterDirectoryChangedCallback_Handle(
		FullScriptPath,
		IDirectoryWatcher::FDirectoryChanged::CreateRaw(this, &FUnrealSharpEditorModule::OnCSharpCodeModified),
		Handle);

	Manager = &UCSManager::GetOrCreate();
	Manager->OnNewStructEvent().AddRaw(this, &FUnrealSharpEditorModule::OnStructRebuilt);
	Manager->OnNewClassEvent().AddRaw(this, &FUnrealSharpEditorModule::OnClassRebuilt);
	Manager->OnNewEnumEvent().AddRaw(this, &FUnrealSharpEditorModule::OnEnumRebuilt);

	FEditorDelegates::ShutdownPIE.AddRaw(this, &FUnrealSharpEditorModule::OnPIEShutdown);

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

	UCSManager& CSharpManager = UCSManager::Get();
	CSharpManager.LoadPluginAssemblyByName(TEXT("UnrealSharp.Editor"));
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

	const UCSUnrealSharpEditorSettings* Settings = GetDefault<UCSUnrealSharpEditorSettings>();

	if (FPlayWorldCommandCallbacks::IsInPIE() && Settings->AutomaticHotReloading == OnScriptSave)
	{
		bHasQueuedHotReload = true;
		return;
	}

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

		if (Settings->AutomaticHotReloading == OnModuleChange && NormalizedFileName.EndsWith(".dll") &&
			NormalizedFileName.Contains(TEXT("/bin/")))
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
			StartHotReload(true);
		}

		return;
	}
}

void FUnrealSharpEditorModule::StartHotReload(bool bRebuild, bool bPromptPlayerWithNewProject)
{
	if (HotReloadStatus == FailedToUnload)
	{
		// If we failed to unload an assembly, we can't hot reload until the editor is restarted.
		bHotReloadFailed = true;
		UE_LOGFMT(LogUnrealSharpEditor, Error, "Hot reload is disabled until the editor is restarted.");
		return;
	}

	TArray<FString> AllProjects;
	FCSProcHelper::GetAllProjectPaths(AllProjects);

	if (AllProjects.IsEmpty())
	{
		if (bPromptPlayerWithNewProject)
		{
			SuggestProjectSetup();
		}

		return;
	}

	HotReloadStatus = Active;
	double StartTime = FPlatformTime::Seconds();

	FScopedSlowTask Progress(3, LOCTEXT("HotReload", "Reloading C#..."));
	Progress.MakeDialog();

	FString SolutionPath = FCSProcHelper::GetPathToSolution();
	FString OutputPath = FCSProcHelper::GetUserAssemblyDirectory();

	const UCSUnrealSharpEditorSettings* Settings = GetDefault<UCSUnrealSharpEditorSettings>();
	FString BuildConfiguration = Settings->GetBuildConfigurationString();
	ECSLoggerVerbosity LogVerbosity = Settings->LogVerbosity;
	
	FString ExceptionMessage;
	if (!ManagedUnrealSharpEditorCallbacks.Build(*SolutionPath, *OutputPath, *BuildConfiguration, &AllProjects, LogVerbosity, &ExceptionMessage, bRebuild))
	{
		HotReloadStatus = Inactive;
		bHotReloadFailed = true;
		FMessageDialog::Open(EAppMsgType::Ok, FText::FromString(ExceptionMessage), FText::FromString(TEXT("Building C# Project Failed")));
		return;
	}

	UCSManager& CSharpManager = UCSManager::Get();
	bool bUnloadFailed = false;

	TArray<FString> ProjectsByLoadOrder;
	FCSProcHelper::GetProjectNamesByLoadOrder(ProjectsByLoadOrder, bDirtyGlue);

	// Unload all assemblies in reverse order to prevent unloading an assembly that is still being referenced.
	// For instance, most assemblies depend on ProjectGlue, so it must be unloaded last.
	// Good info: https://learn.microsoft.com/en-us/dotnet/standard/assembly/unloadability
	// Note: An assembly is only referenced if any of its types are referenced in code.
	// Otherwise optimized out, so ProjectGlue can be unloaded first if it's not used.
	for (int32 i = ProjectsByLoadOrder.Num() - 1; i >= 0; --i)
	{
		const FString& ProjectName = ProjectsByLoadOrder[i];
		TSharedPtr<FCSAssembly> Assembly = CSharpManager.FindAssembly(*ProjectName);

		if (Assembly.IsValid() && !Assembly->UnloadAssembly())
		{
			UE_LOGFMT(LogUnrealSharpEditor, Error, "Failed to unload assembly: {0}", *ProjectName);
			bUnloadFailed = true;
			break;
		}
	}

	if (bUnloadFailed)
	{
		HotReloadStatus = FailedToUnload;
		bHotReloadFailed = true;

		FMessageDialog::Open(EAppMsgType::Ok, LOCTEXT("HotReloadFailure",
		                                              "One or more assemblies failed to unload. Hot reload will be disabled until the editor restarts.\n\n"
		                                              "Possible causes: Strong GC handles, running threads, etc."),
		                     FText::FromString(TEXT("Hot Reload Failed")));

		return;
	}

	// Load all assemblies again in the correct order.
	for (const FString& ProjectName : ProjectsByLoadOrder)
	{
		TSharedPtr<FCSAssembly> Assembly = CSharpManager.FindAssembly(*ProjectName);

		if (Assembly.IsValid())
		{
			Assembly->LoadAssembly();
		}
		else
		{
			// If the assembly is not loaded. It's a new project, and we need to load it.
			CSharpManager.LoadUserAssemblyByName(*ProjectName);
		}
	}

	Progress.EnterProgressFrame(1, LOCTEXT("HotReload", "Refreshing Affected Blueprints..."));
	RefreshAffectedBlueprints();

	HotReloadStatus = Inactive;
	bHotReloadFailed = false;
	bDirtyGlue = false;
	
	UE_LOG(LogUnrealSharpEditor, Log, TEXT("Hot reload took %.2f seconds to execute"), FPlatformTime::Seconds() - StartTime);
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

void FUnrealSharpEditorModule::OnMergeManagedSlnAndNativeSln()
{
	static FString NativeSolutionPath = FPaths::ProjectDir() / FApp::GetProjectName() + ".sln";
	static FString ManagedSolutionPath = FPaths::ConvertRelativePathToFull(FCSProcHelper::GetPathToSolution());

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
	FModuleManager::LoadModuleChecked<ISettingsModule>("Settings").ShowViewer(
		"Editor", "General", "CSUnrealSharpEditorSettings");
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

void FUnrealSharpEditorModule::RepairComponents()
{
	FAssetRegistryModule& AssetRegistryModule = FModuleManager::LoadModuleChecked<FAssetRegistryModule>(
		AssetRegistryConstants::ModuleName);
	AssetRegistryModule.Get().SearchAllAssets(/*bSynchronousSearch =*/true);

	TArray<FAssetData> OutAssetData;
	AssetRegistryModule.Get().GetAssetsByClass(UBlueprint::StaticClass()->GetClassPathName(), OutAssetData, true);

	FScopedSlowTask Progress(OutAssetData.Num());
	Progress.MakeDialog();

	USubobjectDataSubsystem* SubobjectDataSubsystem = GEngine->GetEngineSubsystem<USubobjectDataSubsystem>();

	for (FAssetData const& Asset : OutAssetData)
	{
		const FString AssetPath = Asset.GetObjectPathString();

		if (!AssetPath.Contains(TEXT("/Game/")))
		{
			continue;
		}

		UBlueprint* LoadedBlueprint = Cast<
			UBlueprint>(StaticLoadObject(Asset.GetClass(), nullptr, *AssetPath, nullptr));
		UClass* GeneratedClass = LoadedBlueprint->GeneratedClass;
		UCSClass* ManagedClass = FCSClassUtilities::GetFirstManagedClass(GeneratedClass);

		if (!ManagedClass)
		{
			continue;
		}

		Progress.EnterProgressFrame(1, FText::FromString(FString::Printf(TEXT("Fixing up Blueprint: %s"), *AssetPath)));

		AActor* ActorCDO = Cast<AActor>(GeneratedClass->GetDefaultObject(false));
		if (!ActorCDO)
		{
			continue;
		}

		TArray<FSubobjectDataHandle> SubobjectData;
		SubobjectDataSubsystem->K2_GatherSubobjectDataForBlueprint(LoadedBlueprint, SubobjectData);

		UInheritableComponentHandler* InheritableComponentHandler = LoadedBlueprint->
			GetInheritableComponentHandler(false);

		if (!InheritableComponentHandler)
		{
			continue;
		}

		TArray<UObject*> Subobjects;
		ActorCDO->GetDefaultSubobjects(Subobjects);

		TArray<UObject*> MatchingInstances;
		GetObjectsOfClass(LoadedBlueprint->GeneratedClass, MatchingInstances, true, RF_ClassDefaultObject,
		                  EInternalObjectFlags::Garbage);

		for (TFieldIterator<FObjectProperty> PropertyIt(ManagedClass, EFieldIteratorFlags::IncludeSuper); PropertyIt; ++
		     PropertyIt)
		{
			FObjectProperty* Property = *PropertyIt;

			if (!FCSClassUtilities::IsManagedType(Property->GetOwnerClass()))
			{
				break;
			}

			UActorComponent* OldComponentArchetype = Cast<UActorComponent>(
				Property->GetObjectPropertyValue_InContainer(ActorCDO));

			if (!OldComponentArchetype || !Subobjects.Contains(OldComponentArchetype))
			{
				continue;
			}

			Property->SetObjectPropertyValue_InContainer(ActorCDO, nullptr);

			FComponentKey ComponentKey = InheritableComponentHandler->FindKey(OldComponentArchetype->GetFName());

			if (!ComponentKey.IsValid())
			{
				continue;
			}

			UActorComponent* NewArchetype = InheritableComponentHandler->GetOverridenComponentTemplate(ComponentKey);
			CopyProperties(OldComponentArchetype, NewArchetype);
			FBlueprintEditorUtils::MarkBlueprintAsModified(LoadedBlueprint, Property);

			for (UObject* Instance : MatchingInstances)
			{
				AActor* ActorInstance = static_cast<AActor*>(Instance);
				TArray<TObjectPtr<UActorComponent>>& Components = ActorInstance->BlueprintCreatedComponents;

				for (TObjectPtr<UActorComponent>& Component : Components)
				{
					if (Component->GetName() == OldComponentArchetype->GetName())
					{
						CopyProperties(OldComponentArchetype, Component);
					}
				}
			}
		}

		UBlueprintEditorLibrary::CompileBlueprint(LoadedBlueprint);
	}
}

void FUnrealSharpEditorModule::CopyProperties(UActorComponent* Source, UActorComponent* Target)
{
	UClass* SourceClass = Source->GetClass();
	UClass* TargetClass = Target->GetClass();

	if (SourceClass != TargetClass)
	{
		UE_LOG(LogUnrealSharpEditor, Error, TEXT("Source and Target classes are not the same."));
		return;
	}

	for (TFieldIterator<FProperty> PropertyIt(SourceClass, EFieldIteratorFlags::IncludeSuper); PropertyIt; ++PropertyIt)
	{
		FProperty* Property = *PropertyIt;

		if (!Property->HasAnyPropertyFlags(CPF_BlueprintVisible | CPF_Edit))
		{
			continue;
		}

		FString Data;
		Property->ExportTextItem_InContainer(Data, Source, nullptr, nullptr, PPF_None);
		Property->ImportText_InContainer(*Data, Target, Target, 0);
	}

	Target->PostLoad();
}

void FUnrealSharpEditorModule::OnRepairComponents()
{
	RepairComponents();
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
	Arguments.Add("ArchiveDirectory", QuotePath(ArchiveDirectory));
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
	FString SolutionPath = FPaths::ConvertRelativePathToFull(FCSProcHelper::GetPathToSolution());

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

	MenuBuilder.BeginSection("Tools", LOCTEXT("Tools", "Tools"));

	MenuBuilder.AddMenuEntry(CSCommands.RepairComponents, NAME_None, TAttribute<FText>(), TAttribute<FText>(),
	                         FSlateIcon(FAppStyle::GetAppStyleSetName(), "SourceControl.Actions.Refresh"));

	return MenuBuilder.MakeWidget();
}

void FUnrealSharpEditorModule::OpenNewProjectDialog(const FString& SuggestedProjectName)
{
	TSharedRef<SWindow> AddCodeWindow = SNew(SWindow)
		.Title(LOCTEXT("CreateNewProject", "New C# Project"))
		.SizingRule(ESizingRule::Autosized)
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
	const UCSUnrealSharpEditorSettings* Settings = GetDefault<UCSUnrealSharpEditorSettings>();
	if (Settings->AutomaticHotReloading == OnEditorFocus && !IsHotReloading() && HasPendingHotReloadChanges() &&
		FApp::HasFocus())
	{
		StartHotReload();
	}

	return true;
}

void FUnrealSharpEditorModule::RegisterCommands()
{
	FCSUnrealSharpEditorCommands::Register();
	UnrealSharpCommands = MakeShareable(new FUICommandList);
	UnrealSharpCommands->MapAction(FCSUnrealSharpEditorCommands::Get().CreateNewProject,
	                               FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnCreateNewProject));
	UnrealSharpCommands->MapAction(FCSUnrealSharpEditorCommands::Get().CompileManagedCode,
	                               FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnCompileManagedCode));
	UnrealSharpCommands->MapAction(FCSUnrealSharpEditorCommands::Get().ReloadManagedCode,
	                               FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnReloadManagedCode));
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
	UnrealSharpCommands->MapAction(FCSUnrealSharpEditorCommands::Get().RepairComponents,
	                               FExecuteAction::CreateStatic(&FUnrealSharpEditorModule::OnRepairComponents));

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
			return GetMenuIcon();
		}));

	Section.AddEntry(Entry);
}

void FUnrealSharpEditorModule::OnPIEShutdown(bool IsSimulating)
{
	// Replicate UE behavior, which forces a garbage collection when exiting PIE.
	ManagedUnrealSharpEditorCallbacks.ForceManagedGC();
	
	if (bHasQueuedHotReload)
	{
		bHasQueuedHotReload = false;
		StartHotReload();
	}
}

bool FUnrealSharpEditorModule::FillTemplateFile(const FString& TemplateName, TMap<FString, FString>& Replacements, const FString& Path)
{
	const FString FullFileName = FCSProcHelper::GetPluginDirectory() / TEXT("Templates") / TemplateName + TEXT(".cs.template");

	FString OutTemplate;
	if (FFileHelper::LoadFileToString(OutTemplate, *FullFileName))
	{
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

	return false;
}

void FUnrealSharpEditorModule::OnStructRebuilt(UCSScriptStruct* NewStruct)
{
	RebuiltStructs.Add(NewStruct);
}

void FUnrealSharpEditorModule::OnClassRebuilt(UCSClass* NewClass)
{
	RebuiltClasses.Add(NewClass);
}

void FUnrealSharpEditorModule::OnEnumRebuilt(UCSEnum* NewEnum)
{
	RebuiltEnums.Add(NewEnum);
}

bool FUnrealSharpEditorModule::IsPinAffectedByReload(const FEdGraphPinType& PinType) const
{
	UObject* PinSubCategoryObject = PinType.PinSubCategoryObject.Get();
	if (!IsValid(PinSubCategoryObject) || !Manager->IsManagedField(PinSubCategoryObject))
	{
		return false;
	}

	auto IsPinTypeRebuilt = [this](UObject* PinSubCategoryObject) -> bool
	{
		if (UCSClass* Class = Cast<UCSClass>(PinSubCategoryObject))
		{
			return RebuiltClasses.Contains(Class);
		}

		if (UCSEnum* Enum = Cast<UCSEnum>(PinSubCategoryObject))
		{
			return RebuiltEnums.Contains(Enum);
		}

		if (UCSScriptStruct* Struct = Cast<UCSScriptStruct>(PinSubCategoryObject))
		{
			return RebuiltStructs.Contains(Struct);
		}

		if (UCSEnum* Enum = Cast<UCSEnum>(PinSubCategoryObject))
		{
			return RebuiltEnums.Contains(Enum);
		}

		return false;
	};

	if (!IsPinTypeRebuilt(PinSubCategoryObject))
	{
		return false;
	}

	if (PinType.IsMap() && PinType.PinValueType.TerminalSubCategoryObject.IsValid())
	{
		UObject* MapValueType = PinType.PinValueType.TerminalSubCategoryObject.Get();
		if (IsValid(MapValueType) && Manager->IsManagedField(MapValueType))
		{
			return IsPinTypeRebuilt(MapValueType);
		}
	}

	return false;
}

bool FUnrealSharpEditorModule::IsNodeAffectedByReload(UEdGraphNode* Node) const
{
	if (UK2Node_EditablePinBase* EditableNode = Cast<UK2Node_EditablePinBase>(Node))
	{
		for (const TSharedPtr<FUserPinInfo>& Pin : EditableNode->UserDefinedPins)
		{
			if (IsPinAffectedByReload(Pin->PinType))
			{
				return true;
			}
		}

		return false;
	}

	for (UEdGraphPin* Pin : Node->Pins)
	{
		if (IsPinAffectedByReload(Pin->PinType))
		{
			return true;
		}
	}

	return false;
}

void FUnrealSharpEditorModule::RefreshAffectedBlueprints()
{
	if (RebuiltStructs.IsEmpty() && RebuiltClasses.IsEmpty() && RebuiltEnums.IsEmpty())
	{
		// Early out if nothing has changed its structure.
		return;
	}

	TArray<UBlueprint*> AffectedBlueprints;
	for (TObjectIterator<UBlueprint> BlueprintIt; BlueprintIt; ++BlueprintIt)
	{
		UBlueprint* Blueprint = *BlueprintIt;
		if (!IsValid(Blueprint->GeneratedClass) || FCSClassUtilities::IsManagedType(Blueprint->GeneratedClass))
		{
			return;
		}
		
		TArray<UK2Node*> AllNodes;
		FBlueprintEditorUtils::GetAllNodesOfClass<UK2Node>(Blueprint, AllNodes);

		for (UK2Node* Node : AllNodes)
		{
			if (IsNodeAffectedByReload(Node))
			{
				Node->ReconstructNode();
			}
		}

		AffectedBlueprints.Add(Blueprint);
	}

	for (UBlueprint* Blueprint : AffectedBlueprints)
	{
		FKismetEditorUtilities::CompileBlueprint(Blueprint, EBlueprintCompileOptions::SkipGarbageCollection);
	}

	RebuiltStructs.Reset();
	RebuiltClasses.Reset();
	RebuiltEnums.Reset();

	CollectGarbage(GARBAGE_COLLECTION_KEEPFLAGS);
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
