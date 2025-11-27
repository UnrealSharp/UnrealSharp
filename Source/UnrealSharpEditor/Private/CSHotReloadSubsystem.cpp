#include "CSHotReloadSubsystem.h"

#include "BlueprintCompilationManager.h"
#include "CSManager.h"
#include "CSStyle.h"
#include "CSUnrealSharpEditorSettings.h"
#include "DirectoryWatcherModule.h"
#include "IDirectoryWatcher.h"
#include "K2Node_CallParentFunction.h"
#include "Engine/InheritableComponentHandler.h"
#include "Engine/SCS_Node.h"
#include "Framework/Notifications/NotificationManager.h"
#include "Kismet2/DebuggerCommands.h"
#include "Types/CSEnum.h"
#include "Types/CSScriptStruct.h"
#include "CSProcHelper.h"
#include "Widgets/Notifications/SNotificationList.h"
#include "Utilities/CSUtilities.h"

#define LOCTEXT_NAMESPACE "UCSHotReloadSubsystem"

void UCSHotReloadSubsystem::Initialize(FSubsystemCollectionBase& Collection)
{
	Super::Initialize(Collection);

	EditorModule = &FUnrealSharpEditorModule::Get();
	UCSManager& Manager = UCSManager::Get();
	Manager.OnNewStructEvent().AddUObject(this, &UCSHotReloadSubsystem::OnStructRebuilt);
	Manager.OnNewClassEvent().AddUObject(this, &UCSHotReloadSubsystem::OnClassRebuilt);
	Manager.OnNewEnumEvent().AddUObject(this, &UCSHotReloadSubsystem::OnEnumRebuilt);
	
	TickDelegate = FTickerDelegate::CreateUObject(this, &UCSHotReloadSubsystem::Tick);
	TickDelegateHandle = FTSTicker::GetCoreTicker().AddTicker(TickDelegate);

	FEditorDelegates::ShutdownPIE.AddUObject(this, &UCSHotReloadSubsystem::OnPIEShutdown);

	FString PathToManagedSolution = FCSProcHelper::GetPathToManagedSolution();
	EditorModule->GetManagedUnrealSharpEditorCallbacks().LoadSolutionAsync(*PathToManagedSolution, &OnHotReloadReady_Callback);
	
	PauseHotReload(TEXT("Waiting for initial C# load..."));
	
	RefreshDirectoryWatchers();
}

void UCSHotReloadSubsystem::Deinitialize()
{
	Super::Deinitialize();
	FTSTicker::GetCoreTicker().RemoveTicker(TickDelegateHandle);
}

void UCSHotReloadSubsystem::OnHotReloadReady_Callback()
{
	AsyncTask(ENamedThreads::GameThread, []()
	{
		Get()->OnHotReloadReady();
	});
}

void UCSHotReloadSubsystem::OnHotReloadReady()
{
	ResumeHotReload();
	UE_LOGFMT(LogUnrealSharpEditor, Display, "C# Hot Reload is ready.");
}

FSlateIcon UCSHotReloadSubsystem::GetMenuIcon() const
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

void UCSHotReloadSubsystem::StartHotReload(bool bPromptPlayerWithNewProject)
{
	if (bHotReloadIsPaused)
	{
		UE_LOGFMT(LogUnrealSharpEditor, Verbose, "Hot reload is currently paused. Skipping hot reload.");
		return;
	}
	
	if (HotReloadStatus == FailedToUnload)
	{
		// If we failed to unload an assembly, we can't hot reload until the editor is restarted.
		bHotReloadFailed = true;
		UE_LOGFMT(LogUnrealSharpEditor, Error, "Hot reload is disabled until the editor is restarted.");
		return;
	}

	TArray<FString> AllProjects;
	FCSProcHelper::GetAllProjectPaths(AllProjects);

	if (AllProjects.IsEmpty() && bPromptPlayerWithNewProject)
	{
		EditorModule->SuggestProjectSetup();
		return;
	}
	
	HotReloadStatus = Active;
	double StartTime = FPlatformTime::Seconds();

	FScopedSlowTask Progress(4, LOCTEXT("HotReload", "Reloading C#..."));
	Progress.MakeDialog(false, true);

	FString ExceptionMessage;
	if (!EditorModule->GetManagedUnrealSharpEditorCallbacks().RecompileDirtyProjects(&ExceptionMessage))
	{
		HotReloadStatus = Inactive;
		bHotReloadFailed = true;

		FMessageDialog::Open(EAppMsgType::Ok, FText::FromString(ExceptionMessage), FText::FromString(TEXT("C# Reload Failed")));
		return;
	}

	Progress.EnterProgressFrame(1, LOCTEXT("HotReload_Reloading", "Reloading Assemblies..."));
	
	TArray<UCSManagedAssembly*> AssembliesSortedByDependencies;
	FCSUtilities::SortAssembliesByDependencyOrder(AffectedAssemblies, AssembliesSortedByDependencies);
	
	AffectedAssemblies.Reset();
	
	for (UCSManagedAssembly* Assembly : AssembliesSortedByDependencies)
	{
		if (!IsValid(Assembly))
		{
			UE_LOGFMT(LogUnrealSharpEditor, Warning, "Skipping invalid assembly during hot reload.");
			continue;
		}
		
		if (Assembly->UnloadManagedAssembly())
		{
			continue;
		}
		
		HotReloadStatus = FailedToUnload;
		bHotReloadFailed = true;

		FString ErrorMessage = FString::Printf(
			TEXT("Failed to unload assembly: %s\n\n"
				 "C# Hot Reload has been disabled for the remainder of this editor session.\n\n"
				 "Common causes include:\n"
				 "- Active references preventing unload (strong GC handles)\n"
				 "- Running or unfinished managed threads\n"
				 "- Dependent assemblies still loaded\n"),
			*Assembly->GetAssemblyName().ToString());

		FMessageDialog::Open(EAppMsgType::Ok, FText::FromString(ErrorMessage), LOCTEXT("HotReloadFailure", "C# Hot Reload Failed"));
		return;
	}
	
	for (int32 i = AssembliesSortedByDependencies.Num() - 1; i >= 0; --i)
	{
		UCSManagedAssembly* Assembly = AssembliesSortedByDependencies[i];
		
		if (!IsValid(Assembly))
		{
			continue;
		}
		
		Assembly->LoadManagedAssembly(true);
	}

	Progress.EnterProgressFrame(1, LOCTEXT("HotReload_Refreshing", "Refreshing Blueprints..."));
	RefreshAffectedBlueprints();

	Progress.EnterProgressFrame(1, LOCTEXT("HotReload_GC", "Performing Garbage Collection..."));
	CollectGarbage(GARBAGE_COLLECTION_KEEPFLAGS, true);
	
	HotReloadStatus = Inactive;
	bHotReloadFailed = false;
	
	UE_LOG(LogUnrealSharpEditor, Log, TEXT("Hot reload took %.2f seconds to execute"), FPlatformTime::Seconds() - StartTime);
}

void UCSHotReloadSubsystem::RefreshAffectedBlueprints()
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSHotReloadSubsystem::RefreshAffectedBlueprints);

	if (RebuiltTypes.IsEmpty())
	{
		return;
	}
	
	for (TObjectIterator<UBlueprint> BlueprintIt; BlueprintIt; ++BlueprintIt)
	{
		UBlueprint* Blueprint = *BlueprintIt;
		UClass* GeneratedClass = Blueprint->GeneratedClass;
		
		if (!IsValid(GeneratedClass) || FCSClassUtilities::IsManagedClass(GeneratedClass))
		{
			continue;
		}
		
		TArray<UEdGraph*> Graphs;
		Blueprint->GetAllGraphs(Graphs);
		
		bool bNeedsRecompile = false;
		for (const UEdGraph* Graph : Graphs)
		{
			for (const TObjectPtr Node : Graph->Nodes)
			{
				if (!IsNodeAffectedByReload(Node))
				{
					continue;
				}

				Node->ReconstructNode();
				bNeedsRecompile = true;
			}
		}

		if (!bNeedsRecompile)
		{
			for (const FBPVariableDescription& VarDesc : Blueprint->NewVariables)
			{
				if (!IsPinAffectedByReload(VarDesc.VarType))
				{
					continue;
				}
				
				bNeedsRecompile = true;
				break;
			}
		}

		if (!bNeedsRecompile)
		{
			UClass* ParentClass = Blueprint->ParentClass;
			while (IsValid(ParentClass))
			{
				if (UCSManager::Get().IsManagedType(ParentClass))
				{
					int32 TypeID = ParentClass->GetUniqueID();
					
					if (RebuiltTypes.ContainsByHash(TypeID, TypeID))
					{
						bNeedsRecompile = true;
						break;
					}
				}

				ParentClass = ParentClass->GetSuperClass();
			}
		}
		
		if (!bNeedsRecompile && GeneratedClass->IsChildOf(AActor::StaticClass()))
		{
			bNeedsRecompile = HasDefaultComponentsBeenAffected(Blueprint);
		}
		
		if (!bNeedsRecompile)
		{
			continue;
		}

		FKismetEditorUtilities::CompileBlueprint(Blueprint, EBlueprintCompileOptions::SkipGarbageCollection);
	}
	
	RebuiltTypes.Reset();
}

bool UCSHotReloadSubsystem::IsPinAffectedByReload(const FEdGraphPinType& PinType) const
{
	UObject* PinSubCategoryObject = PinType.PinSubCategoryObject.Get();
	
	if (!IsValid(PinSubCategoryObject) || !UCSManager::Get().IsManagedType(PinSubCategoryObject))
	{
		return false;
	}

	uint32 TypeID = PinSubCategoryObject->GetUniqueID();
	if (RebuiltTypes.ContainsByHash(TypeID, TypeID))
	{
		return true;
	}

	if (PinType.PinValueType.TerminalSubCategoryObject.IsValid())
	{
		UObject* MapValueType = PinType.PinValueType.TerminalSubCategoryObject.Get();
		
		if (IsValid(MapValueType) && UCSManager::Get().IsManagedType(MapValueType))
		{
			return RebuiltTypes.ContainsByHash(MapValueType->GetUniqueID(), MapValueType->GetUniqueID());
		}
	}

	return false;
}

bool UCSHotReloadSubsystem::IsNodeAffectedByReload(const UEdGraphNode* Node) const
{
	if (const UK2Node_EditablePinBase* EditableNode = Cast<UK2Node_EditablePinBase>(Node))
	{
		for (const TSharedPtr<FUserPinInfo>& Pin : EditableNode->UserDefinedPins)
		{
			if (IsPinAffectedByReload(Pin->PinType))
			{
				return true;
			}
		}
	}

	if (const UK2Node_CallParentFunction* CallParentFunction = Cast<UK2Node_CallParentFunction>(Node))
	{
		UFunction* EventFunction = CallParentFunction->GetTargetFunction();
		
		if (IsValid(EventFunction))
		{
			UClass* OwnerClass = EventFunction->GetOwnerClass();

			if (!IsValid(OwnerClass) || !UCSManager::Get().IsManagedType(OwnerClass))
			{
				return false;
			}
			
			if (UCSSkeletonClass* SkeletonClass = Cast<UCSSkeletonClass>(OwnerClass))
			{
				OwnerClass = SkeletonClass->GetGeneratedClass();
			}

			int32 TypeID = OwnerClass->GetUniqueID();
			
			if (RebuiltTypes.ContainsByHash(TypeID, TypeID))
			{
				return true;
			}
		}
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

bool UCSHotReloadSubsystem::HasDefaultComponentsBeenAffected(const UBlueprint* Blueprint) const
{
	auto CheckIfTemplateIsAffected = [&](const UClass* TemplateClass) -> bool
	{
		if (!IsValid(TemplateClass) || !UCSManager::Get().IsManagedType(TemplateClass))
		{
			return false;
		}

		int32 TypeID = TemplateClass->GetUniqueID();
		if (RebuiltTypes.ContainsByHash(TypeID, TypeID))
		{
			return true;
		}
		
		return false;
	};

	USimpleConstructionScript* SCS = Blueprint->SimpleConstructionScript;
	if (IsValid(SCS))
	{
		for (const USCS_Node* Node : SCS->GetAllNodes())
		{
			if (!CheckIfTemplateIsAffected(Node->ComponentClass))
			{
				continue;
			}

			return true;
		}
	}

	UInheritableComponentHandler* ComponentHandler = Blueprint->InheritableComponentHandler;
	if (IsValid(ComponentHandler))
	{
		TArray<UActorComponent*> OutArray;
		ComponentHandler->GetAllTemplates(OutArray);

		for (UActorComponent* ComponentTemplate : OutArray)
		{
			if (!CheckIfTemplateIsAffected(ComponentTemplate->GetClass()))
			{
				continue;
			}

			return true;
		}
	}

	return false;
}

void UCSHotReloadSubsystem::OnStructRebuilt(UCSScriptStruct* NewStruct)
{
	AddRebuiltType(NewStruct);
}

void UCSHotReloadSubsystem::OnClassRebuilt(UCSClass* NewClass)
{
	AddRebuiltType(NewClass);
}

void UCSHotReloadSubsystem::OnEnumRebuilt(UCSEnum* NewEnum)
{
	AddRebuiltType(NewEnum);
}

void UCSHotReloadSubsystem::RefreshDirectoryWatchers()
{
	TArray<FString> ProjectPaths;
	FCSProcHelper::GetAllProjectPaths(ProjectPaths, true);

	for (const FString& ProjectPath : ProjectPaths)
	{
		FString Path = FPaths::GetPath(ProjectPath);
		AddDirectoryToWatch(Path);
	}
}

void UCSHotReloadSubsystem::OnPIEShutdown(bool IsSimulating)
{
	// Replicate UE behavior, which forces a garbage collection when exiting PIE.
	EditorModule->GetManagedUnrealSharpEditorCallbacks().ForceManagedGC();

	if (bHasQueuedHotReload)
	{
		bHasQueuedHotReload = false;
		StartHotReload();
	}
}

bool UCSHotReloadSubsystem::Tick(float DeltaTime)
{
	const UCSUnrealSharpEditorSettings* Settings = GetDefault<UCSUnrealSharpEditorSettings>();
	if (Settings->AutomaticHotReloading == OnEditorFocus && !IsHotReloading() && HasPendingHotReloadChanges() &&FApp::HasFocus())
	{
		StartHotReload();
	}

	return true;
}

void UCSHotReloadSubsystem::AddDirectoryToWatch(const FString& Directory)
{
	if (WatchingDirectories.Contains(Directory))
	{
		return;
	}
	
	if (!FPaths::DirectoryExists(Directory))
	{
		FPlatformFileManager::Get().GetPlatformFile().CreateDirectory(*Directory);
	}

	FDirectoryWatcherModule& DirectoryWatcherModule = FModuleManager::LoadModuleChecked<FDirectoryWatcherModule>("DirectoryWatcher");
	
	FDelegateHandle Handle;
	DirectoryWatcherModule.Get()->RegisterDirectoryChangedCallback_Handle(
		Directory,
		IDirectoryWatcher::FDirectoryChanged::CreateUObject(this, &UCSHotReloadSubsystem::OnScriptsFolderChanged),
		Handle);

	WatchingDirectories.Add(Directory);
}

void UCSHotReloadSubsystem::PauseHotReload(const FString& Reason)
{
	if (bHotReloadIsPaused)
	{
		return;
	}
	
	FString NotificationFormat = FString::Printf(TEXT("C# Reload Paused: %s"), *Reason);
	PauseNotification = MakeNotification(NotificationFormat);
	bHotReloadIsPaused = true;
}

void UCSHotReloadSubsystem::ResumeHotReload()
{
	if (!bHotReloadIsPaused)
	{
		return;
	}

	bHotReloadIsPaused = false;
	
	if (PauseNotification.IsValid())
	{
		PauseNotification->SetText(LOCTEXT("HotReloadResumed", "C# Reload Resumed"));
		PauseNotification->SetCompletionState(SNotificationItem::CS_Success);
		PauseNotification->ExpireAndFadeout();
		PauseNotification.Reset();
	}
	
	if (FileChangesDuringPause.Num() > 0)
	{
		OnScriptsFolderChanged(FileChangesDuringPause);
		FileChangesDuringPause.Empty();
	}
}

void FindProjectFileFromChangedFile(const FFileChangeData& ChangedFile, FString& OutProjectFile)
{
	FString Directory = FPaths::GetPath(ChangedFile.Filename);
	while (!Directory.IsEmpty())
	{
		TArray<FString> FoundFiles;
		IFileManager::Get().FindFiles(FoundFiles, *Directory, TEXT("*.csproj"));

		if (FoundFiles.Num() > 0)
		{
			OutProjectFile = FPaths::GetBaseFilename(FoundFiles[0]);
			return;
		}
		
		Directory = FPaths::GetPath(Directory);
	}
}

void UCSHotReloadSubsystem::OnScriptsFolderChanged(const TArray<FFileChangeData>& ChangedFiles)
{
	if (bHotReloadIsPaused)
	{
		FileChangesDuringPause.Append(ChangedFiles);
		return;
	}
	
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
	
	struct FCSChangedFile
	{
		FString ProjectFile;
		FString FilePath;
		
		const FFileChangeData& ChangeData;
	};

	bool bHotReloadNeeded = false;
	TArray<FCSChangedFile> DirtiedFiles;
	FString ExceptionMessage;
	
	for (const FFileChangeData& ChangedFile : ChangedFiles)
	{
		FString NormalizedFileName = ChangedFile.Filename;

		// Skip generated files in bin and obj folders
		if (NormalizedFileName.Contains(TEXT("/obj/")) || NormalizedFileName.Contains(TEXT("/bin/")))
		{
			continue;
		}

		// Check if the file is a .cs file and not in the bin directory
		FString Extension = FPaths::GetExtension(NormalizedFileName);
		if (Extension != "cs")
		{
			continue;
		}
		
		if (Settings->AutomaticHotReloading != OnScriptSave)
		{
			HotReloadStatus = PendingReload;
		}
		else
		{
			bHotReloadNeeded = true;
		}
		
		FString FileName = FPaths::GetCleanFilename(NormalizedFileName);
		NormalizedFileName.ReplaceInline(TEXT("/"), TEXT("\\"));
		
		FString ProjectFile;
		FindProjectFileFromChangedFile(ChangedFile, ProjectFile);
		
		if (ProjectFile.IsEmpty())
		{
			ExceptionMessage = FString::Printf(TEXT("Could not find .csproj file for changed file: %s"), *FileName);
			break;
		}
		
		DirtiedFiles.Add({ ProjectFile, NormalizedFileName, ChangedFile });
		
		UCSManagedAssembly* Assembly = UCSManager::Get().FindAssembly(*ProjectFile);
		if (!IsValid(Assembly))
		{
			ExceptionMessage = FString::Printf(TEXT("Could not find loaded assembly for project: %s"), *ProjectFile);
			break;
		}
		
		if (AffectedAssemblies.Contains(Assembly))
		{
			continue;
		}
		
		AffectedAssemblies.Add(Assembly);
	}
	
	DirtiedFiles.Sort([](const FCSChangedFile& A, const FCSChangedFile& B)
	{
		return A.ChangeData.Action == FFileChangeData::FCA_Removed && B.ChangeData.Action != FFileChangeData::FCA_Removed;
	});
	
	FCSManagedUnrealSharpEditorCallbacks& UnrealSharpEditorCallbacks = EditorModule->GetManagedUnrealSharpEditorCallbacks();
	
	for (const FCSChangedFile& DirtiedFile : DirtiedFiles)
	{
		if (DirtiedFile.ChangeData.Action == FFileChangeData::FCA_Removed)
		{
			UnrealSharpEditorCallbacks.RemoveSourceFile(*DirtiedFile.ProjectFile, *DirtiedFile.FilePath);
		}
		else
		{
			UnrealSharpEditorCallbacks.RecompileChangedFile(*DirtiedFile.ProjectFile, *DirtiedFile.FilePath, &ExceptionMessage);
		}
	}

	if (!ExceptionMessage.IsEmpty())
	{
		FMessageDialog::Open(EAppMsgType::Ok, FText::FromString(ExceptionMessage), FText::FromString(TEXT("C# Hot Reload Error")));
		return;
	}

	if (bHotReloadNeeded)
	{
		StartHotReload();
	}
}

TSharedPtr<SNotificationItem> UCSHotReloadSubsystem::MakeNotification(const FString& Text) const
{
	FNotificationInfo Info(FText::FromString(Text));
	Info.Image = GetMenuIcon().GetIcon();
	Info.bFireAndForget = false;
	Info.FadeOutDuration = 0.0f;
	Info.ExpireDuration = 0.0f;

	TSharedPtr<SNotificationItem> NewNotification = FSlateNotificationManager::Get().AddNotification(Info);
	NewNotification->SetCompletionState(SNotificationItem::CS_Pending);
	return NewNotification;
}

#undef LOCTEXT_NAMESPACE
