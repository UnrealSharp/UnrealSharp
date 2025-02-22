#include "CSBlueprintUpdater.h"
#include "BlueprintCompilationManager.h"
#include "CSManager.h"
#include "K2Node_MacroInstance.h"
#include "Kismet2/BlueprintEditorUtils.h"
#include "TypeGenerator/Register/CSGeneratedClassBuilder.h"

FCSBlueprintUpdater& FCSBlueprintUpdater::Get()
{
	static FCSBlueprintUpdater Instance;
	return Instance;
}

void FCSBlueprintUpdater::Initialize()
{
	UCSManager::Get().OnNewStructEvent().AddRaw(this, &FCSBlueprintUpdater::OnNewStruct);
}

void FCSBlueprintUpdater::OnNewStruct(UScriptStruct* NewStruct)
{
	UpdatedStructs.Add(NewStruct);
}

bool FCSBlueprintUpdater::TryUpdatePin(const FEdGraphPinType& PinType) const
{
	UObject* PinSubCategoryObject = PinType.PinSubCategoryObject.Get();
	
	if (PinType.PinCategory == UEdGraphSchema_K2::PC_Struct)
	{
		UScriptStruct* Struct = Cast<UScriptStruct>(PinSubCategoryObject);
		return UpdatedStructs.Contains(Struct);
	}
	
	if (PinType.IsMap())
	{
		if (UScriptStruct* Struct = Cast<UScriptStruct>(PinSubCategoryObject))
		{
			return UpdatedStructs.Contains(Struct);
		}
		
		UObject* MapValueType = PinType.PinValueType.TerminalSubCategoryObject.Get();
		if (UScriptStruct* Struct = Cast<UScriptStruct>(MapValueType))
		{
			return UpdatedStructs.Contains(Struct);
		}
	}
	else if (PinType.IsSet() || PinType.IsArray())
	{
		UScriptStruct* Struct = Cast<UScriptStruct>(PinSubCategoryObject);
		return UpdatedStructs.Contains(Struct);
	}

	return false;
}

bool FCSBlueprintUpdater::UpdateNodePinTypes(UEdGraphNode* Node) const
{
	if (UK2Node_EditablePinBase* EditableNode = Cast<UK2Node_EditablePinBase>(Node))
	{
		for (const TSharedPtr<FUserPinInfo>& Pin : EditableNode->UserDefinedPins)
		{
			if (TryUpdatePin(Pin->PinType))
			{
				return true;
			}
		}
	}

	for (UEdGraphPin* Pin : Node->Pins)
	{
		if (TryUpdatePin(Pin->PinType))
		{
			return true;
		}
	}

	return false;
}

void FCSBlueprintUpdater::UpdateBlueprints()
{
	if (UpdatedStructs.IsEmpty())
	{
		return;
	}
	
	TSet<UBlueprint*> ToUpdate;
	for (TObjectIterator<UBlueprint> BlueprintIt; BlueprintIt; ++BlueprintIt)
	{
		UBlueprint* Blueprint = *BlueprintIt;
		if (!IsValid(Blueprint->GeneratedClass) || FCSGeneratedClassBuilder::IsManagedType(Blueprint->GeneratedClass))
		{
			continue;
		}

		bool BlueprintHasBeenUpdated = false;

		TArray<UK2Node*> AllNodes;
		FBlueprintEditorUtils::GetAllNodesOfClass(Blueprint, AllNodes);
		
		for (UK2Node* Node : AllNodes)
		{
			if (UpdateNodePinTypes(Node))
			{
				Node->ReconstructNode();
				BlueprintHasBeenUpdated = true;
			}
		}

		if (BlueprintHasBeenUpdated)
		{
			ToUpdate.Add(Blueprint);
		}
	}
	
	for (UBlueprint* Blueprint : ToUpdate)
	{
		FBlueprintCompilationManager::QueueForCompilation(Blueprint);
	}
		
	FBlueprintCompilationManager::FlushCompilationQueueAndReinstance();
	UpdatedStructs.Empty();
}
