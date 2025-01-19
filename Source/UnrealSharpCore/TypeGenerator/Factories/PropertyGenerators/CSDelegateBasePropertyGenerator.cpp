#include "CSDelegateBasePropertyGenerator.h"

#include "K2Node_FunctionEntry.h"
#include "Kismet2/BlueprintEditorUtils.h"
#include "TypeGenerator/Factories/CSPropertyFactory.h"
#include "TypeGenerator/Register/MetaData/CSDelegateMetaData.h"

FEdGraphPinType UCSDelegateBasePropertyGenerator::MakeDelegate(FName DelegateType, const FCSPropertyMetaData& PropertyMetaData, UBlueprint* Blueprint)
{
	FEdGraphPinType PinType;
	PinType.PinCategory = DelegateType;
	UClass* Class = CastChecked<UClass>(Blueprint->GeneratedClass);
	
	PinType.PinSubCategoryMemberReference.MemberName = PropertyMetaData.Name;
	PinType.PinSubCategoryMemberReference.MemberGuid = ConstructGUIDFromName(PropertyMetaData.Name);
	PinType.PinSubCategoryMemberReference.MemberParent = Class;
	
	const UEdGraphSchema_K2* K2Schema = GetDefault<UEdGraphSchema_K2>();
	
	UEdGraph* DelegateGraph = nullptr;
	for (UEdGraph* Graph : Blueprint->DelegateSignatureGraphs)
	{
		if (Graph->GetFName() == PropertyMetaData.Name)
		{
			DelegateGraph = Graph;
			break;
		}
	}
	
	if (DelegateGraph == nullptr)
	{
		DelegateGraph = FBlueprintEditorUtils::CreateNewGraph(Blueprint, PropertyMetaData.Name, UEdGraph::StaticClass(), UEdGraphSchema_K2::StaticClass());
		DelegateGraph->bEditable = false;

		K2Schema->CreateDefaultNodesForGraph(*DelegateGraph);
		K2Schema->CreateFunctionGraphTerminators(*DelegateGraph, (UClass*)nullptr);
		K2Schema->AddExtraFunctionFlags(DelegateGraph, (FUNC_BlueprintCallable|FUNC_BlueprintEvent|FUNC_Public));
		K2Schema->MarkFunctionEntryAsEditable(DelegateGraph, true);
		
		Blueprint->DelegateSignatureGraphs.Add(DelegateGraph);
	}

	TArray<UK2Node_FunctionEntry*> EntryNodes;
	DelegateGraph->GetNodesOfClass(EntryNodes);
	
	UK2Node_EditablePinBase* EntryNode = EntryNodes[0];
	int32 NumPins = EntryNode->Pins.Num();
	for (int32 i = NumPins - 1; i >= 0; --i)
	{
		EntryNode->RemovePin(EntryNode->Pins[i]);
	}

	TSharedPtr<FCSDelegateMetaData> MulticastDelegateMetaData = PropertyMetaData.GetTypeMetaData<FCSDelegateMetaData>();
	const TArray<FCSPropertyMetaData>& Parameters = MulticastDelegateMetaData->SignatureFunction.Parameters;

	for (const FCSPropertyMetaData& Parameter : Parameters)
	{
		UCSPropertyGenerator* PropertyGenerator = FCSPropertyFactory::FindPropertyGenerator(Parameter.Type->PropertyType);
		FEdGraphPinType ParameterPinType = PropertyGenerator->GetPinType(Parameter.Type->PropertyType, Parameter, Blueprint);
		EntryNode->CreateUserDefinedPin(Parameter.Name, ParameterPinType, EGPD_Input);
	}

	return PinType;
}
