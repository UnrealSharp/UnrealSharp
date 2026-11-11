#pragma once

#include "CoreMinimal.h"
#include "Containers/Ticker.h"

class FBlueprintEditor;
class UBlueprint;
struct FToolMenuContext;

class FCSBlueprintEditorCustomization
{
public:
	FCSBlueprintEditorCustomization();
	~FCSBlueprintEditorCustomization();

private:
	struct FEditorState
	{
		TWeakPtr<FBlueprintEditor> Editor;
		TWeakObjectPtr<UBlueprint> Blueprint;
		TWeakObjectPtr<UClass> Parent;
		FDelegateHandle ChangedHandle;
		FDelegateHandle CompiledHandle;
		bool bOwnsOverlay = false;
		bool bPending = false;
	};

	void OnMenuGenerating(FName Name, const FToolMenuContext& Context);
	void TrackEditor(const TSharedRef<FBlueprintEditor>& Editor);
	void OnBlueprintChanged(UBlueprint* Blueprint);
	void QueueUpdate(FEditorState& State);
	bool UpdateOverlays(float DeltaTime);
	void Unbind(FEditorState& State);

	TArray<FEditorState> Editors;
	FDelegateHandle MenuGeneratingHandle;
	FTSTicker::FDelegateHandle UpdateHandle;
};
