#include "HotReload/CSHotReloadSubsystem.h"
#include "CSManager.h"
#include "CSStyle.h"
#include "CSUnrealSharpEditorSettings.h"
#include "DirectoryWatcherModule.h"
#include "IDirectoryWatcher.h"
#include "Kismet2/DebuggerCommands.h"
#include "Types/CSEnum.h"
#include "Types/CSScriptStruct.h"
#include "CSProcUtilities.h"
#include "HotReload/CSHotReloadUtilities.h"
#include "Utilities/CSAssemblyUtilities.h"
#include "Utilities/CSEditorUtilities.h"
#include "Widgets/Notifications/SNotificationList.h"

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
	
	FString PathToManagedSolution = UCSProcUtilities::GetPathToManagedSolution();
	UnrealSharpEditorModule->GetManagedEditorCallbacks().LoadSolutionAsync(*PathToManagedSolution, &OnHotReloadReady_Callback);
	
	RefreshDirectoryWatchers();
	
	PauseHotReload(TEXT("Waiting for initial C# load..."));
}

void UCSHotReloadSubsystem::Deinitialize()
{
	Super::Deinitialize();
	FTSTicker::GetCoreTicker().RemoveTicker(HotReloadTickDelegate);
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

bool UCSHotReloadSubsystem::HasPendingHotReloadChanges() const
{
	bool bHasPendingChanges = false;
	for (const UCSManagedAssembly* Assembly : PendingModifiedAssemblies)
	{
		if (!IsValid(Assembly) || FCSAssemblyUtilities::IsGlueAssembly(Assembly))
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
	if (FPlayWorldCommandCallbacks::IsInPIE())
	{
		UE_LOGFMT(LogUnrealSharpEditor, Verbose, "Cannot perform C# hot reload while in PIE.");
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
	
	if (CurrentHotReloadStatus == FailedToUnload)
	{
		// If we failed to unload an assembly, we can't hot reload until the editor is restarted.
		UE_LOGFMT(LogUnrealSharpEditor, Error, "Hot reload is disabled until the editor is restarted.");
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
		FMessageDialog::Open(EAppMsgType::Ok, FText::FromString(ExceptionMessage), FText::FromString(TEXT("C# Reload Failed")));
		return;
	}
	
	PendingModifiedAssemblies.Reset();

	Progress.EnterProgressFrame(1, LOCTEXT("HotReload_Reloading", "Reloading Assemblies..."));
	
	for (UCSManagedAssembly* Assembly : AssembliesSortedByDependencies)
	{
		if (Assembly->UnloadManagedAssembly())
		{
			continue;
		}
		
		CurrentHotReloadStatus = FailedToUnload;

		FString ErrorMessage = FString::Printf(
			TEXT("Failed to unload assembly: %s\n\n"
				"C# Hot Reload has been disabled for the remainder of this editor session.\n\n"
				"Common causes include:\n"
				"- Active references preventing unload (strong GC handles)\n"
				"- Running or unfinished managed threads\n"
				"- Dependent assemblies still loaded\n"), *Assembly->GetAssemblyName().ToString());

		FMessageDialog::Open(EAppMsgType::Ok, FText::FromString(ErrorMessage), LOCTEXT("HotReloadFailure", "C# Hot Reload Failed"));
		return;
	}
	
	for (int32 i = AssembliesSortedByDependencies.Num() - 1; i >= 0; --i)
	{
		AssembliesSortedByDependencies[i]->LoadManagedAssembly();
	}

	Progress.EnterProgressFrame(1, LOCTEXT("HotReload_Refreshing", "Refreshing Affected Blueprints..."));
	
	FCSHotReloadUtilities::RebuildDependentBlueprints(ReloadedTypes);
	
	if (bDetectedNewManagedType)
	{
		FCSHotReloadUtilities::RefreshPlacementMode();
	}

	Progress.EnterProgressFrame(1, LOCTEXT("HotReload_GC", "Performing Garbage Collection..."));
	CollectGarbage(GARBAGE_COLLECTION_KEEPFLAGS);
	
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

void UCSHotReloadSubsystem::RefreshDirectoryWatchers()
{
	TArray<FString> ProjectPaths;
	UCSProcUtilities::GetAllProjectPaths(ProjectPaths, true);

	for (const FString& ProjectPath : ProjectPaths)
	{
		FString Path = FPaths::GetPath(ProjectPath);
		FName ProjectName = *FPaths::GetBaseFilename(ProjectPath);
		AddDirectoryToWatch(Path, ProjectName);
	}
}

void UCSHotReloadSubsystem::DirtyUnrealType(const char* AssemblyName, const char* Namespace, const char* TypeName)
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
	
	ManagedTypeDefinition->MarkStructurallyDirty();
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
	PauseNotification = FCSEditorUtilities::MakeNotification(GetMenuIcon(), NotificationFormat);
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
}

void UCSHotReloadSubsystem::HandleScriptFileChanges(const TArray<FFileChangeData>& ChangedFiles, FName ProjectName)
{
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
	
	UCSManagedAssembly* ModifiedAssembly = UCSManager::Get().FindOrLoadAssembly(ProjectName);
	if (!IsValid(ModifiedAssembly))
	{
		UE_LOGFMT(LogUnrealSharpEditor, Warning, "Could not find assembly for project {0} during hot reload.", *ProjectName.ToString());
		return;
	}
	
	if (!PendingModifiedAssemblies.Contains(ModifiedAssembly))
	{
		PendingModifiedAssemblies.Add(ModifiedAssembly);
	}
	
	if (FCSHotReloadUtilities::ShouldDeferHotReloadRequest(ModifiedAssembly))
	{
		UE_LOGFMT(LogUnrealSharpEditor, Verbose, "Deferring hot reload request for assembly {0}.", *ModifiedAssembly->GetAssemblyName().ToString());
		return;
	}
	
	PerformHotReload();
}

#undef LOCTEXT_NAMESPACE
