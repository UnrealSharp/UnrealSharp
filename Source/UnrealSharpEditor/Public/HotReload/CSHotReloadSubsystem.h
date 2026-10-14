#pragma once

#include "CoreMinimal.h"
#include "CSManagedTypeDefinition.h"
#include "CSObjectID.h"
#include "EditorSubsystem.h"
#include "UnrealSharpEditor.h"
#include "IDirectoryWatcher.h"
#include "CSHotReloadSubsystem.generated.h"

UENUM(BlueprintType)
enum class EHotReloadStatus : uint8
{
	// Not Hot Reloading
	Inactive,
	// Actively Hot Reloading
	Active,
	// Failed to compile the managed code during Hot Reload
	FailedToCompile
};

UCLASS(BlueprintType)
class UCSHotReloadSubsystem : public UEditorSubsystem
{
	GENERATED_BODY()
public:

	// USubsystem interface implementation
	virtual void Initialize(FSubsystemCollectionBase& Collection) override;
	virtual bool ShouldCreateSubsystem(UObject* Outer) const override;
	virtual void Deinitialize() override;
	// End of interface

	static UCSHotReloadSubsystem* Get()
	{
		if (!IsValid(GEditor))
		{
			return nullptr;
		}
		
		return GEditor->GetEditorSubsystem<UCSHotReloadSubsystem>();
	}

	UFUNCTION(BlueprintPure, Category = "UnrealSharp | Hot Reload")
	UNREALSHARPEDITOR_API bool IsHotReloading() const { return CurrentHotReloadStatus == EHotReloadStatus::Active; }

	UFUNCTION(BlueprintPure, Category = "UnrealSharp | Hot Reload")
	UNREALSHARPEDITOR_API bool HasPendingHotReloadChanges() const;
	
	/** Reloads pending changes when ready and outside PIE/SIE. */
	UFUNCTION(BlueprintCallable, Category = "UnrealSharp | Hot Reload")
	UNREALSHARPEDITOR_API void PerformHotReload();

	UFUNCTION(BlueprintPure, Category = "UnrealSharp | Hot Reload")
	UNREALSHARPEDITOR_API EHotReloadStatus GetHotReloadStatus() const { return CurrentHotReloadStatus; }

	/** Hot reload is paused while the managed solution is loading or glue is being generated. */
	UFUNCTION(BlueprintPure, Category = "UnrealSharp | Hot Reload")
	UNREALSHARPEDITOR_API bool IsHotReloadPaused() const { return bIsHotReloadPaused; }
	
	void PauseHotReload(const FString& Reason = FString());
	void ResumeHotReload();
	
	void RefreshDirectoryWatchers();
	void NotifyNewType() { bDetectedNewManagedType = true;}

private:
	
	void ReportHotReloadError(const FString& Message, const FText& Title);

	void AddDirectoryToWatch(const FString& Directory, FName ProjectName);
	
	void HandleScriptFileChanges(const TArray<FFileChangeData>& ChangedFiles, FName ProjectName);

	static void OnHotReloadReady_Callback();
	void OnHotReloadReady();

	void OnStructRebuilt(UCSScriptStruct* NewStruct);
	void OnClassRebuilt(UCSClass* NewClass);
	void OnEnumRebuilt(UCSEnum* NewEnum);
	void OnInterfaceRebuilt(UCSInterface* NewInterface);
	
	void AppendPendingFileChange(const TArray<FFileChangeData>& ChangedFiles, FName ProjectName);
	
	void AddReloadedType(const UObject* NewType)
	{
		uint32 TypeID = NewType->GetUniqueID();
		ReloadedTypes.AddByHash(TypeID, TypeID);
	}

	void OnStopPlayingPIE(bool IsSimulating);
	bool Tick(float DeltaTime);

	UPROPERTY(Transient)
	TArray<TObjectPtr<UCSManagedAssembly>> PendingModifiedAssemblies;

	FTickerDelegate HotReloadTickHandle;
	FTSTicker::FDelegateHandle HotReloadTickDelegate;

	TSharedPtr<SNotificationItem> PauseNotification;

	FUnrealSharpEditorModule* UnrealSharpEditorModule = nullptr;

	EHotReloadStatus CurrentHotReloadStatus = EHotReloadStatus::Inactive;
	bool bIsHotReloadPaused = false;

	TArray<FString> WatchingDirectories;

	TMap<FName, TArray<FFileChangeData>> PendingFileChanges;

	TSet<FCSObjectID> ReloadedTypes;
	bool bDetectedNewManagedType = false;
};
