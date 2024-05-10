#include "CSReinstancer.h"
#include "BlueprintActionDatabase.h"
#include "AssetRegistry/AssetRegistryModule.h"
#include "CSharpForUE/TypeGenerator/Register/CSTypeRegistry.h"
#include "Kismet2/BlueprintEditorUtils.h"
#include "Kismet2/ReloadUtilities.h"

FCSReinstancer& FCSReinstancer::Get()
{
	static FCSReinstancer Instance;
	return Instance;
}

void FCSReinstancer::Initialize()
{
	FCSTypeRegistry::Get().GetOnNewClassEvent().AddRaw(this, &FCSReinstancer::AddPendingClass);
	FCSTypeRegistry::Get().GetOnNewStructEvent().AddRaw(this, &FCSReinstancer::AddPendingStruct);
	FCSTypeRegistry::Get().GetOnNewEnumEvent().AddRaw(this, &FCSReinstancer::AddPendingEnum);
}

void FCSReinstancer::AddPendingClass(UClass* OldClass, UClass* NewClass)
{
	ClassesToReinstance.Add(MakeTuple(OldClass, NewClass));
}

void FCSReinstancer::AddPendingStruct(UScriptStruct* OldStruct, UScriptStruct* NewStruct)
{
	StructsToReinstance.Add(MakeTuple(OldStruct, NewStruct));
}

void FCSReinstancer::AddPendingEnum(UEnum* OldEnum, UEnum* NewEnum)
{
	EnumsToReinstance.Add(MakeTuple(OldEnum, NewEnum));
}

void FCSReinstancer::AddPendingInterface(UClass* OldInterface, UClass* NewInterface)
{
	InterfacesToReinstance.Add(MakeTuple(OldInterface, NewInterface));
}


void FCSReinstancer::TryUpdatePin(FEdGraphPinType& PinType)
{
	UObject* PinSubCategoryObject = PinType.PinSubCategoryObject.Get();
	
	if (PinType.PinCategory == UEdGraphSchema_K2::PC_Struct)
	{
		UScriptStruct* Struct = Cast<UScriptStruct>(PinSubCategoryObject);
		if (UScriptStruct** FoundStruct = StructsToReinstance.Find(Struct))
		{
			PinType.PinSubCategoryObject = *FoundStruct;
		}
	}
	else if (PinType.PinCategory == UEdGraphSchema_K2::PC_Enum || PinType.PinCategory == UEdGraphSchema_K2::PC_Byte)
	{
		UEnum* Enum = Cast<UEnum>(PinSubCategoryObject);
		if (UEnum** FoundEnum = EnumsToReinstance.Find(Enum))
		{
			PinType.PinSubCategoryObject = *FoundEnum;
		}
	}
	else if (PinType.PinSubCategory == UEdGraphSchema_K2::PC_Class
		|| PinType.PinSubCategory == UEdGraphSchema_K2::PC_Object
		|| PinType.PinSubCategory == UEdGraphSchema_K2::PC_SoftObject
		|| PinType.PinSubCategory == UEdGraphSchema_K2::PC_SoftClass)
	{
		UClass* Interface = Cast<UClass>(PinSubCategoryObject);
		if (UClass** FoundInterface = InterfacesToReinstance.Find(Interface))
		{
			PinType.PinSubCategoryObject = *FoundInterface;
		}
	}
}

void FCSReinstancer::StartReinstancing()
{
	TUniquePtr<FReload> Reload = MakeUnique<FReload>(EActiveReloadType::Reinstancing, TEXT(""), *GWarn);

	auto NotifyChanges = [&Reload](const auto& Container)
	{
		for (const auto& [Old, New] : Container)
		{
			if (!Old || !New)
			{
				continue;
			}

			Reload->NotifyChange(New, Old);
		}
	};

	NotifyChanges(InterfacesToReinstance);
	NotifyChanges(StructsToReinstance);
	NotifyChanges(EnumsToReinstance);
	NotifyChanges(ClassesToReinstance);

	// Before we reinstance, we want the BP to know about the new types
	UpdateBlueprints();
	
	Reload->Reinstance();
	PostReinstance();

	IAssetRegistry& AssetRegistry = FModuleManager::LoadModuleChecked<FAssetRegistryModule>(TEXT("AssetRegistry")).Get();
	
	auto CleanOldTypes = [](const auto& Container, IAssetRegistry& AssetRegistry)
	{
		for (const auto& [Old, New] : Container)
		{
			if (!Old || !New)
			{
				continue;
			}

			AssetRegistry.AssetDeleted(Old);
			Old->SetFlags(RF_NewerVersionExists);
			Old->RemoveFromRoot();
		}
	};

	CleanOldTypes(InterfacesToReinstance, AssetRegistry);
	CleanOldTypes(StructsToReinstance, AssetRegistry);
	CleanOldTypes(EnumsToReinstance, AssetRegistry);
	CleanOldTypes(ClassesToReinstance, AssetRegistry);

	InterfacesToReinstance.Empty();
	StructsToReinstance.Empty();
	EnumsToReinstance.Empty();
	ClassesToReinstance.Empty();

	Reload->Finalize(true);
	EndReload();
}

void FCSReinstancer::PostReinstance()
{
	FBlueprintActionDatabase& ActionDB = FBlueprintActionDatabase::Get();
	for (auto& Element : ClassesToReinstance)
	{
		ActionDB.ClearAssetActions(Element.Key);
		ActionDB.RefreshClassActions(Element.Value);
	}

	for (auto& Struct : StructsToReinstance)
	{
		TArray<UDataTable*> Tables;
		GetTablesDependentOnStruct(Struct.Key, Tables);
		
		for (UDataTable*& Table : Tables)
		{
			auto Data = Table->GetTableAsJSON();
			Struct.Key->StructFlags = static_cast<EStructFlags>(STRUCT_NoDestructor | Struct.Key->StructFlags);
			Table->CleanBeforeStructChange();
			Table->RowStruct = Struct.Value;
			Table->CreateTableFromJSONString(Data);
		}
	}
	
	for (const auto& StructToReinstancePair : StructsToReinstance)
	{
		StructToReinstancePair.Key->ChildProperties = nullptr;
		StructToReinstancePair.Key->ConditionalBeginDestroy();
	}
	
	if (FPropertyEditorModule* PropertyModule = FModuleManager::GetModulePtr<FPropertyEditorModule>("PropertyEditor"))
	{
		PropertyModule->NotifyCustomizationModuleChanged();
	}

	if (!GEngine)
	{
		return;
	}
	
	GEditor->BroadcastBlueprintCompiled();	

	auto* World = GEditor->GetEditorWorldContext().World();
	GEngine->Exec( World, TEXT("MAP REBUILD ALLVISIBLE") );
}

void FCSReinstancer::UpdateBlueprints()
{
	for (TObjectIterator<UBlueprint> BlueprintIt; BlueprintIt; ++BlueprintIt)
	{
		UBlueprint* Blueprint = *BlueprintIt;
		for (FBPVariableDescription& NewVariable : Blueprint->NewVariables)
		{
			TryUpdatePin(NewVariable.VarType);
		}

		TArray<UK2Node_EditablePinBase*> AllNodes;
		FBlueprintEditorUtils::GetAllNodesOfClass(Blueprint, AllNodes);

		for (UK2Node_EditablePinBase* Node : AllNodes)
		{
			for (const TSharedPtr<FUserPinInfo>& Pin : Node->UserDefinedPins)
			{
				TryUpdatePin(Pin->PinType);
			}
		}
	}
}

void FCSReinstancer::GetTablesDependentOnStruct(UScriptStruct* Struct, TArray<UDataTable*>& DataTables)
{
	TArray<UDataTable*> Result;
	if (Struct)
	{
		TArray<UObject*> FoundDataTables;
		GetObjectsOfClass(UDataTable::StaticClass(), FoundDataTables);
		for (UObject* DataTableObj : DataTables)
		{
			UDataTable* DataTable = Cast<UDataTable>(DataTableObj);
			if (DataTable && Struct == DataTable->RowStruct)
			{
				Result.Add(DataTable);
			}
		}
	}
}
