#pragma once

#include "CoreMinimal.h"
#include "EditorSubsystem.h"
#include "UnrealSharpEditor.h"
#include "CSHotReloadSubsystem.generated.h"

class FControlFlowNode;
class FControlFlow;

UCLASS(MinimalAPI)
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
		return GEditor->GetEditorSubsystem<UCSHotReloadSubsystem>();
	}

	FSlateIcon GetMenuIcon() const;

	UNREALSHARPEDITOR_API bool IsHotReloading() const { return HotReloadStatus == Active; }
	UNREALSHARPEDITOR_API bool HasPendingHotReloadChanges() const { return HotReloadStatus == PendingReload; }
	UNREALSHARPEDITOR_API bool HasHotReloadFailed() const { return bHotReloadFailed; }

	UNREALSHARPEDITOR_API void StartHotReload(bool bPromptPlayerWithNewProject = true);

	void AddDirectoryToWatch(const FString& Directory);
	
	void PauseHotReload(const FString& Reason = FString());
	void ResumeHotReload();

	void RefreshDirectoryWatchers();

private:

	void OnScriptDirectoryChanged(const TArray<struct FFileChangeData>& ChangedFiles);
	
	TSharedPtr<SNotificationItem> MakeNotification(const FString& Text) const;

	static void OnHotReloadReady_Callback();
	void OnHotReloadReady();

	void RefreshAffectedBlueprints();

	bool IsPinAffectedByReload(const FEdGraphPinType& PinType) const;
	bool IsNodeAffectedByReload(const UEdGraphNode* Node) const;

	void OnStructRebuilt(UCSScriptStruct* NewStruct);
	void OnClassRebuilt(UCSClass* NewClass);
	void OnEnumRebuilt(UCSEnum* NewEnum);
	
	void AddRebuiltType(const UObject* NewType)
	{
		uint32 TypeID = NewType->GetUniqueID();
		RebuiltTypes.AddByHash(TypeID, TypeID);
	}

	void OnPIEShutdown(bool IsSimulating);

	bool Tick(float DeltaTime);

	TSharedPtr<SNotificationItem> PauseNotification;
	FUnrealSharpEditorModule* EditorModule = nullptr;

	HotReloadStatus HotReloadStatus = Inactive;
	
	bool bHasHotReloadInitialized = false;
	bool bHotReloadFailed = false;
	bool bHasQueuedHotReload = false;

	FTickerDelegate TickDelegate;
	FTSTicker::FDelegateHandle TickDelegateHandle;

	TSharedPtr<FControlFlow> HotReloadControlFlow;

	TArray<FString> WatchingDirectories;
	
	TSet<uint32> RebuiltTypes;
	bool HotReloadIsPaused = false;
};
