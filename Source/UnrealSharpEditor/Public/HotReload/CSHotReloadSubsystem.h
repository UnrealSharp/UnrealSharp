#pragma once

#include "CoreMinimal.h"
#include "EditorSubsystem.h"
#include "UnrealSharpEditor.h"
#include "IDirectoryWatcher.h"
#include "CSHotReloadSubsystem.generated.h"

enum EHotReloadStatus : uint8
{
	// Not Hot Reloading
	Inactive,
	// Actively Hot Reloading
	Active,
	// Failed to unload an assembly during Hot Reload
	FailedToUnload,
	// Failed to compile the managed code during Hot Reload
	FailedToCompile
};

struct FCSPendingHotReloadChange
{
	FCSPendingHotReloadChange(FName InProjectName, const TArray<FFileChangeData>& InChangedFiles)
		: ProjectName(InProjectName)
		, ChangedFiles(InChangedFiles)
	{
	}
	
	FName ProjectName;
	TArray<FFileChangeData> ChangedFiles;
};

UCLASS()
class UCSHotReloadSubsystem : public UEditorSubsystem
{
	GENERATED_BODY()
public:

	// USubsystem interface implementation
	virtual void Initialize(FSubsystemCollectionBase& Collection) override;
	virtual bool ShouldCreateSubsystem(UObject* Outer) const override { return !FApp::IsUnattended(); }
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

	FSlateIcon GetMenuIcon() const;

	UNREALSHARPEDITOR_API bool IsHotReloading() const { return CurrentHotReloadStatus == Active; }
	UNREALSHARPEDITOR_API bool HasPendingHotReloadChanges() const;
	UNREALSHARPEDITOR_API bool HasHotReloadFailed() const { return CurrentHotReloadStatus == FailedToUnload || CurrentHotReloadStatus == FailedToCompile; }
	
	UNREALSHARPEDITOR_API void PerformHotReload();
	
	void PauseHotReload(const FString& Reason = FString());
	void ResumeHotReload();
	
	void RefreshDirectoryWatchers();
	
	void DirtyUnrealType(const char* AssemblyName, const char* Namespace, const char* TypeName);

private:
	
	void AddDirectoryToWatch(const FString& Directory, FName ProjectName);
	
	void HandleScriptFileChanges(const TArray<FFileChangeData>& ChangedFiles, FName ProjectName);

	static void OnHotReloadReady_Callback();
	void OnHotReloadReady();

	void OnStructRebuilt(UCSScriptStruct* NewStruct);
	void OnClassRebuilt(UCSClass* NewClass);
	void OnEnumRebuilt(UCSEnum* NewEnum);
	void OnInterfaceRebuilt(UCSInterface* NewInterface);
	
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

	EHotReloadStatus CurrentHotReloadStatus = Inactive;
	bool bIsHotReloadPaused = false;

	TArray<FString> WatchingDirectories;

	TArray<FCSPendingHotReloadChange> PendingFileChanges;

	TSet<uint32> ReloadedTypes;
	bool bDetectedNewManagedType = false;
};
