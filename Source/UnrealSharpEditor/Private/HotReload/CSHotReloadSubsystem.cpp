#include "HotReload/CSHotReloadSubsystem.h"

#include "CSInstallationUtilities.h"
#include "CSManager.h"
#include "CSStyle.h"
#include "CSUnrealSharpEditorSettings.h"
#include "DirectoryWatcherModule.h"
#include "IDirectoryWatcher.h"
#include "Kismet2/DebuggerCommands.h"
#include "Types/CSEnum.h"
#include "Types/CSScriptStruct.h"
#include "CSPathsUtilities.h"
#include "CSProjectUtilities.h"
#include "HotReload/CSHotReloadUtilities.h"
#include "Kismet2/StructureEditorUtils.h"
#include "Utilities/CSAssemblyUtilities.h"
#include "Utilities/CSEditorUtilities.h"
#include "Widgets/Notifications/SNotificationList.h"
#include "UObject/UObjectGlobals.h"
#include "UObject/UnrealType.h"

#define LOCTEXT_NAMESPACE "UCSHotReloadSubsystem"

void UCSHotReloadSubsystem::Initialize(FSubsystemCollectionBase& Collection)
{
	Super::Initialize(Collection);
	UCSManager& Manager = UCSManager::Get();
	Manager.OnNewStructEvent().AddUObject(this, &UCSHotReloadSubsystem::OnStructRebuilt);
	Manager.OnNewClassEvent().AddUObject(this, &UCSHotReloadSubsystem::OnClassRebuilt);
	Manager.OnNewEnumEvent().AddUObject(this, &UCSHotReloadSubsystem::OnEnumRebuilt);
	Manager.OnNewInterfaceEvent().AddUObject(this, &UCSHotReloadSubsystem::OnInterfaceRebuilt);
	
	HotReloadTickHandle = FTickerDelegate::CreateUObject(this, &UCSHotReloadSubsystem::Tick);
	HotReloadTickDelegate = FTSTicker::GetCoreTicker().AddTicker(HotReloadTickHandle);

	FEditorDelegates::ShutdownPIE.AddUObject(this, &UCSHotReloadSubsystem::OnStopPlayingPIE);

	UnrealSharpEditorModule = &FUnrealSharpEditorModule::Get();
	
	FString PathToManagedSolution = UnrealSharp::Paths::GetPathToManagedSolution();
	UnrealSharpEditorModule->GetManagedEditorCallbacks().LoadSolutionAsync(*PathToManagedSolution, (void*)&OnHotReloadReady_Callback);
	
	RefreshDirectoryWatchers();
	
	PauseHotReload(TEXT("Waiting for initial C# load..."));
}

bool UCSHotReloadSubsystem::ShouldCreateSubsystem(UObject* Outer) const
{
	return !FApp::IsUnattended() && !IsRunningCommandlet() && UnrealSharp::InstallationUtilities::IsDotNetSdkInstalled();
}

void UCSHotReloadSubsystem::Deinitialize()
{
	PendingManagedObjectRecovery.Reset();
	PreservedManagedObjectData.Reset();
	Super::Deinitialize();
	FTSTicker::GetCoreTicker().RemoveTicker(HotReloadTickDelegate);
}

void UCSHotReloadSubsystem::TransferManagedObjectRecovery(const TMap<UObject*, UObject*>& Replacements)
{
	for (const auto& Pair : Replacements)
	{
		if (PendingManagedObjectRecovery.Remove(TWeakObjectPtr<UObject>(Pair.Key)) && IsValid(Pair.Value))
		{
			PendingManagedObjectRecovery.Add(Pair.Value);
		}
	}
}

void UCSHotReloadSubsystem::TrackManagedObjectsForRecovery(const TArray<UCSManagedAssembly*>& Assemblies)
{
	UCSManager& Manager = UCSManager::Get();
	for (auto It = PendingManagedObjectRecovery.CreateIterator(); It; ++It)
	{
		if (!It->IsValid())
		{
			It.RemoveCurrent();
		}
	}
	// Only track objects with existing managed handles from affected assemblies.
	for (const auto& Pair : Manager.GetManagedObjectHandles())
	{
		UObject* Object = Pair.Key.GetUObject();
		if (IsValid(Object) && Assemblies.Contains(Manager.FindOwningAssembly(Object->GetClass())))
		{
			PendingManagedObjectRecovery.Add(Object);
		}
	}
}

void UCSHotReloadSubsystem::RecoverManagedObjects()
{
	UCSManager& Manager = UCSManager::Get();
	PreservedManagedObjectData.Reset();
	for (const TWeakObjectPtr<UObject>& Key : PendingManagedObjectRecovery)
	{
		UObject* Object = Key.Get();
		if (!IsValid(Object) || Object->GetClass()->HasAnyClassFlags(CLASS_NewerVersionExists))
		{
			continue;
		}
		const TSharedPtr<FGCHandle>* Handle = Manager.GetManagedObjectHandles().Find(FCSObjectID(Object));
		if (Handle && Handle->IsValid() && !(*Handle)->IsNull())
		{
			continue;
		}
		UCSManagedAssembly* Assembly = Manager.FindOwningAssembly(Object->GetClass());
		if (!IsValid(Assembly) || !Assembly->IsAssemblyLoaded())
		{
			continue;
		}

		// Capture current values after rebuilding, including unsaved edits and new defaults.
		TUniquePtr<FStructOnScope> Snapshot = MakeUnique<FStructOnScope>(Object->GetClass());
		for (TFieldIterator<FProperty> It(Object->GetClass(), EFieldIteratorFlags::IncludeSuper); It; ++It)
		{
			It->CopyCompleteValue_InContainer(Snapshot->GetStructMemory(), Object);
		}
		PreservedManagedObjectData.Add(Key, MoveTemp(Snapshot));
	}

	// Snapshot the whole recovery set first: one constructor can access another object.
	for (const auto& Pair : PreservedManagedObjectData)
	{
		if (UObject* Object = Pair.Key.Get())
		{
			Manager.FindManagedObject(Object);
		}
	}
	for (const auto& Pair : PreservedManagedObjectData)
	{
		if (UObject* Object = Pair.Key.Get())
		{
			for (TFieldIterator<FProperty> It(Object->GetClass(), EFieldIteratorFlags::IncludeSuper); It; ++It)
			{
				It->CopyCompleteValue_InContainer(Object, Pair.Value->GetStructMemory());
			}
		}
	}
	PendingManagedObjectRecovery.Reset();
	PreservedManagedObjectData.Reset();
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

bool UCSHotReloadSubsystem::HasPendingHotReloadChanges() const
{
	bool bHasPendingChanges = false;
	for (const UCSManagedAssembly* Assembly : PendingModifiedAssemblies)
	{
		if (FCSAssemblyUtilities::IsRuntimeGlueAssembly(Assembly))
		{
			continue;
		}
		
		bHasPendingChanges = true;
		break;
	}
	
	return bHasPendingChanges;
}

void UCSHotReloadSubsystem::PerformHotReload()
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSHotReloadSubsystem::PerformHotReload)
	
	if (FPlayWorldCommandCallbacks::IsInPIE() || FPlayWorldCommandCallbacks::IsInSIE())
	{
		UE_LOGFMT(LogUnrealSharpEditor, Verbose, "Cannot perform C# hot reload while in PIE or SIE.");
		return;
	}
	
	if (!HasPendingHotReloadChanges())
	{
		UE_LOGFMT(LogUnrealSharpEditor, Verbose, "No pending C# changes to hot reload.");
		return;
	}
	
	if (IsHotReloading())
	{
		UE_LOGFMT(LogUnrealSharpEditor, Warning, "A hot reload is already in progress. Skipping hot reload request.");
		return;
	}
	
	if (bIsHotReloadPaused)
	{
		UE_LOGFMT(LogUnrealSharpEditor, Verbose, "Hot reload is currently paused. Skipping hot reload.");
		return;
	}
	
	UE_LOGFMT(LogUnrealSharpEditor, Display, "Starting C# Hot Reload...");
	
	CurrentHotReloadStatus = Active;
	double StartTime = FPlatformTime::Seconds();

	FScopedSlowTask Progress(4, LOCTEXT("HotReload", "Reloading C#..."));
	Progress.MakeDialog(false, true);
	
	TArray<UCSManagedAssembly*> AssembliesSortedByDependencies;
	FCSAssemblyUtilities::SortAssembliesByDependencyOrder(PendingModifiedAssemblies, AssembliesSortedByDependencies);

	FString ExceptionMessage;
	if (!FCSHotReloadUtilities::RecompileDirtyProjects(AssembliesSortedByDependencies, ExceptionMessage))
	{
		CurrentHotReloadStatus = FailedToCompile;
		FMessageDialog::Open(EAppMsgType::Ok, FText::FromString(ExceptionMessage), FText::FromString(TEXT("C# Compilation Failed")));
		return;
	}
	
	PendingModifiedAssemblies.Reset();

	Progress.EnterProgressFrame(1, LOCTEXT("HotReload_Reloading", "Reloading Assemblies..."));

	TrackManagedObjectsForRecovery(AssembliesSortedByDependencies);
	
	for (UCSManagedAssembly* Assembly : AssembliesSortedByDependencies)
	{
		Assembly->UnloadAssembly();
	}
	
	for (int32 i = AssembliesSortedByDependencies.Num() - 1; i >= 0; --i)
	{
		AssembliesSortedByDependencies[i]->LoadAssembly();
	}

	Progress.EnterProgressFrame(1, LOCTEXT("HotReload_Refreshing", "Refreshing Affected Blueprints..."));
	
	FCSHotReloadUtilities::RebuildDependentBlueprints(ReloadedTypes);
	
	if (bDetectedNewManagedType)
	{
		FCSHotReloadUtilities::RefreshPlacementMode();
		FCSHotReloadUtilities::RefreshBlueprintActionDatabase(ReloadedTypes);
	}
	
	if (ReloadedTypes.Num() > 0)
	{
		FCSHotReloadUtilities::RefreshStructs(ReloadedTypes);
	}

	RecoverManagedObjects();

	if (ReloadedTypes.Num() > 0)
	{
		Progress.EnterProgressFrame(1, LOCTEXT("HotReload_GC", "Performing Garbage Collection..."));
		CollectGarbage(GARBAGE_COLLECTION_KEEPFLAGS);
	}
	
	CurrentHotReloadStatus = Inactive;
	bDetectedNewManagedType = false;
	ReloadedTypes.Reset();
	
	UE_LOG(LogUnrealSharpEditor, Display, TEXT("C# Hot Reload completed in %.2f seconds."), FPlatformTime::Seconds() - StartTime);
}

void UCSHotReloadSubsystem::OnStructRebuilt(UCSScriptStruct* NewStruct)
{
	AddReloadedType(NewStruct);
}

void UCSHotReloadSubsystem::OnClassRebuilt(UCSClass* NewClass)
{
	AddReloadedType(NewClass);
}

void UCSHotReloadSubsystem::OnEnumRebuilt(UCSEnum* NewEnum)
{
	AddReloadedType(NewEnum);
}

void UCSHotReloadSubsystem::OnInterfaceRebuilt(UCSInterface* NewInterface)
{
	AddReloadedType(NewInterface);
}

void UCSHotReloadSubsystem::AppendPendingFileChange(const TArray<FFileChangeData>& ChangedFiles, FName ProjectName)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSHotReloadSubsystem::AppendPendingFileChange)
	
	TArray<FFileChangeData>& PendingChangesForProject = PendingFileChanges.FindOrAdd(ProjectName);
	
	for (const FFileChangeData& ChangeData : ChangedFiles)
	{
		bool bAlreadyPending = PendingChangesForProject.ContainsByPredicate([&ChangeData](const FFileChangeData& PendingChange)
		{
			if (PendingChange.Filename == ChangeData.Filename && PendingChange.Action == ChangeData.Action)
			{
				return true;
			}
			
			return false;
		});
		
		if (!bAlreadyPending)
		{
			PendingChangesForProject.Add(ChangeData);
		}
	}
}

void UCSHotReloadSubsystem::RefreshDirectoryWatchers()
{
	TArray<FString> ProjectPaths;
	UnrealSharp::Project::GetAllProjectPaths(ProjectPaths);

	for (const FString& ProjectPath : ProjectPaths)
	{
		FString Path = FPaths::GetPath(ProjectPath);
		FName ProjectName = *FPaths::GetBaseFilename(ProjectPath);
		AddDirectoryToWatch(Path, ProjectName);
	}
}

void UCSHotReloadSubsystem::DirtyUnrealType(const char* AssemblyName, const char* Namespace, const char* TypeName, ECSTypeStructuralFlags Flags)
{
	UCSManagedAssembly* Assembly = UCSManager::Get().FindAssembly(AssemblyName);

	if (!IsValid(Assembly))
	{
		return;
	}

	FCSFieldName FieldName(TypeName, Namespace);
	TSharedPtr<FCSManagedTypeDefinition> ManagedTypeDefinition = Assembly->FindManagedTypeDefinition(FieldName);

	if (!ManagedTypeDefinition.IsValid())
	{
		bDetectedNewManagedType = true;
		UE_LOGFMT(LogUnrealSharpEditor, Verbose, "Skipping dirty check: {0}.{1} isn't registered in assembly {2}. It may be a new managed type.", Namespace, TypeName, AssemblyName);
		return;
	}
	
	ManagedTypeDefinition->SetDirtyFlags(Flags);
}

void UCSHotReloadSubsystem::OnStopPlayingPIE(bool IsSimulating)
{
	// Replicate UE behavior, which forces a garbage collection when exiting PIE.
	UnrealSharpEditorModule->GetManagedEditorCallbacks().ForceManagedGC();
	
	if (GetDefault<UCSUnrealSharpEditorSettings>()->AutomaticHotReloading != Off)
	{
		PerformHotReload();
	}
}

bool UCSHotReloadSubsystem::Tick(float DeltaTime)
{
	if (FCSHotReloadUtilities::ShouldHotReloadOnEditorFocus(this))
	{
		PerformHotReload();
	}
	
	return true;
}

void UCSHotReloadSubsystem::AddDirectoryToWatch(const FString& Directory, FName ProjectName)
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
		IDirectoryWatcher::FDirectoryChanged::CreateUObject(this, &UCSHotReloadSubsystem::HandleScriptFileChanges, ProjectName),
		Handle);

	WatchingDirectories.Add(Directory);
}

void UCSHotReloadSubsystem::PauseHotReload(const FString& Reason)
{
	if (bIsHotReloadPaused)
	{
		return;
	}
	
	FString NotificationFormat = FString::Printf(TEXT("C# Reload Paused: %s"), *Reason);
	PauseNotification = FCSEditorUtilities::MakeNotification(UnrealSharp::Icons::GetUnrealSharpIcon(), NotificationFormat);
	bIsHotReloadPaused = true;
}

void UCSHotReloadSubsystem::ResumeHotReload()
{
	if (!bIsHotReloadPaused)
	{
		return;
	}

	bIsHotReloadPaused = false;
	
	if (PauseNotification.IsValid())
	{
		PauseNotification->SetText(LOCTEXT("HotReloadResumed", "C# Reload Resumed"));
		PauseNotification->SetCompletionState(SNotificationItem::CS_Success);
		PauseNotification->ExpireAndFadeout();
		PauseNotification.Reset();
	}
	
	for (const TPair<FName, TArray<FFileChangeData>>& Pair : PendingFileChanges)
	{
		HandleScriptFileChanges(Pair.Value, Pair.Key);
	}
}

void UCSHotReloadSubsystem::HandleScriptFileChanges(const TArray<FFileChangeData>& ChangedFiles, FName ProjectName)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(UCSHotReloadSubsystem::HandleScriptFileChanges)
	
	if (IsHotReloading())
	{
		return;
	}
	
	TArray<FFileChangeData> CSharpFiles;
	FCSHotReloadUtilities::GetChangedCSharpFiles(ChangedFiles, CSharpFiles);
	
	if (CSharpFiles.IsEmpty())
	{
		return;
	}
	
	if (bIsHotReloadPaused)
	{
		AppendPendingFileChange(ChangedFiles, ProjectName);
		return;
	}
	
	TArray<FCSHotReloadUtilities::FCSChangedFile> DirtiedFiles;
	FCSHotReloadUtilities::CollectDirtiedFiles(CSharpFiles, DirtiedFiles);
	
	if (DirtiedFiles.IsEmpty())
	{
		return;
	}
	
	FString ExceptionMessage;
	if (!FCSHotReloadUtilities::ApplyDirtiedFiles(ProjectName.ToString(), DirtiedFiles, ExceptionMessage))
	{
		FMessageDialog::Open(EAppMsgType::Ok, FText::FromString(ExceptionMessage), FText::FromString(TEXT("C# Hot Reload Error")));
		return;
	}
	
	UCSManagedAssembly* ModifiedAssembly = UCSManager::Get().FindAssembly(ProjectName);
	if (!PendingModifiedAssemblies.Contains(ModifiedAssembly))
	{
		PendingModifiedAssemblies.Add(ModifiedAssembly);
	}
	
	if (FCSHotReloadUtilities::ShouldDeferHotReloadRequest(ModifiedAssembly))
	{
		UE_LOGFMT(LogUnrealSharpEditor, Verbose, "Deferring hot reload request for assembly {0}.", *ModifiedAssembly->GetName());
		return;
	}
	
	PerformHotReload();
}

#undef LOCTEXT_NAMESPACE
