#include "CSBlueprintEditorCustomization.h"

#include "BlueprintEditor.h"
#include "BlueprintEditorContext.h"
#include "BlueprintEditorModule.h"
#include "CSParentClassControls.h"
#include "Engine/Blueprint.h"
#include "ToolMenus.h"
#include "Types/CSClass.h"

FCSBlueprintEditorCustomization::FCSBlueprintEditorCustomization()
{
	MenuGeneratingHandle = UToolMenus::Get()->OnPreGenerateWidget.AddRaw(this, &FCSBlueprintEditorCustomization::OnMenuGenerating);
	for (const TSharedRef<IBlueprintEditor>& Editor : FModuleManager::GetModuleChecked<FBlueprintEditorModule>(TEXT("Kismet")).GetBlueprintEditors())
	{
		TrackEditor(StaticCastSharedRef<FBlueprintEditor>(Editor));
	}
}

FCSBlueprintEditorCustomization::~FCSBlueprintEditorCustomization()
{
	if (UToolMenus* Menus = UToolMenus::TryGet())
	{
		Menus->OnPreGenerateWidget.Remove(MenuGeneratingHandle);
	}
	FTSTicker::GetCoreTicker().RemoveTicker(UpdateHandle);
	for (FEditorState& State : Editors)
	{
		Unbind(State);
		if (State.bOwnsOverlay)
		{
			if (TSharedPtr<FBlueprintEditor> Editor = State.Editor.Pin())
			{
				Editor->RegenerateMenusAndToolbars();
			}
		}
	}
}

void FCSBlueprintEditorCustomization::Unbind(FEditorState& State)
{
	if (UBlueprint* Blueprint = State.Blueprint.Get())
	{
		Blueprint->OnChanged().Remove(State.ChangedHandle);
		Blueprint->OnCompiled().Remove(State.CompiledHandle);
	}
}

void FCSBlueprintEditorCustomization::OnMenuGenerating(FName Name, const FToolMenuContext& Context)
{
	const UBlueprintEditorToolMenuContext* BlueprintContext = Context.FindContext<UBlueprintEditorToolMenuContext>();
	if (BlueprintContext)
	{
		TSharedPtr<FBlueprintEditor> Editor = BlueprintContext->BlueprintEditor.Pin();
		if (Editor && Name == Editor->GetToolMenuName())
		{
			TrackEditor(Editor.ToSharedRef());
		}
	}
}

void FCSBlueprintEditorCustomization::TrackEditor(const TSharedRef<FBlueprintEditor>& Editor)
{
	for (int32 Index = Editors.Num() - 1; Index >= 0; --Index)
	{
		if (!Editors[Index].Editor.IsValid())
		{
			Unbind(Editors[Index]);
			Editors.RemoveAtSwap(Index);
		}
	}
	FEditorState* State = Editors.FindByPredicate([&Editor](const FEditorState& Item) { return Item.Editor.Pin() == Editor; });
	if (!State)
	{
		UBlueprint* Blueprint = Editor->GetBlueprintObj();
		if (!Blueprint)
		{
			return;
		}
		State = &Editors.AddDefaulted_GetRef();
		State->Editor = Editor;
		State->Blueprint = Blueprint;
		State->ChangedHandle = Blueprint->OnChanged().AddRaw(this, &FCSBlueprintEditorCustomization::OnBlueprintChanged);
		State->CompiledHandle = Blueprint->OnCompiled().AddRaw(this, &FCSBlueprintEditorCustomization::OnBlueprintChanged);
	}
	QueueUpdate(*State);
}

void FCSBlueprintEditorCustomization::OnBlueprintChanged(UBlueprint* Blueprint)
{
	for (FEditorState& State : Editors)
	{
		if (State.Blueprint == Blueprint && State.Parent.Get() != Blueprint->ParentClass)
		{
			QueueUpdate(State);
		}
	}
}

void FCSBlueprintEditorCustomization::QueueUpdate(FEditorState& State)
{
	State.bPending = true;
	if (!UpdateHandle.IsValid())
	{
		// Kismet installs its overlay after generating menus. Replace it on the next tick.
		UpdateHandle = FTSTicker::GetCoreTicker().AddTicker(FTickerDelegate::CreateRaw(this, &FCSBlueprintEditorCustomization::UpdateOverlays));
	}
}

bool FCSBlueprintEditorCustomization::UpdateOverlays(float DeltaTime)
{
	UpdateHandle.Reset();
	TArray<TWeakPtr<FBlueprintEditor>> RestoreEditors;
	for (FEditorState& State : Editors)
	{
		if (!State.bPending)
		{
			continue;
		}
		State.bPending = false;
		TSharedPtr<FBlueprintEditor> Editor = State.Editor.Pin();
		UBlueprint* Blueprint = State.Blueprint.Get();
		if (!Editor || !Blueprint)
		{
			continue;
		}
		State.Parent = Blueprint->ParentClass;
		if (const UCSClass* Parent = Cast<UCSClass>(Blueprint->ParentClass); Parent && Parent->HasManagedTypeDefinition())
		{
			State.bOwnsOverlay = true;
			Editor->SetMenuOverlay(MakeCSParentClassControls(Blueprint));
		}
		else if (State.bOwnsOverlay)
		{
			State.bOwnsOverlay = false;
			RestoreEditors.Add(Editor);
		}
	}
	// Menu generation reenters TrackEditor, which can remove expired records.
	for (const TWeakPtr<FBlueprintEditor>& WeakEditor : RestoreEditors)
	{
		if (TSharedPtr<FBlueprintEditor> Editor = WeakEditor.Pin())
		{
			Editor->RegenerateMenusAndToolbars();
		}
	}
	return false;
}
