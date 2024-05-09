#include "CSReinstancer.h"

#include "BlueprintActionDatabase.h"
#include "K2Node_MacroInstance.h"
#include "CSharpForUE/TypeGenerator/Register/CSTypeRegistry.h"
#include "Kismet2/BlueprintEditorUtils.h"
#include "Kismet2/ReloadUtilities.h"
#include "Serialization/ArchiveReplaceObjectRef.h"

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

void FCSReinstancer::UpdatePins(UStruct* Struct)
{
	for (TFieldIterator<FProperty> PropIt(Struct, EFieldIterationFlags::None); PropIt; ++PropIt)
	{
		FProperty* Prop = *PropIt;

		if (FEnumProperty* EnumProperty = CastField<FEnumProperty>(Prop))
		{
			if (UEnum** Enum = EnumsToReinstance.Find(EnumProperty->GetEnum()))
			{
				EnumProperty->SetEnum(*Enum);
			}
		}
		if (FByteProperty* ByteProperty = CastField<FByteProperty>(Prop))
		{
			if (UEnum** Enum = EnumsToReinstance.Find(ByteProperty->Enum))
			{
				ByteProperty->Enum = *Enum;
			}
		}
		else if (FStructProperty* StructProperty = CastField<FStructProperty>(Prop))
		{
			if (UScriptStruct** Struct = StructsToReinstance.Find(StructProperty->Struct))
			{
				StructProperty->Struct = *Struct;
			}
		}
		else if (FObjectProperty* ObjectProperty = CastField<FObjectProperty>(Prop))
		{
			if (UClass** Interface = ClassesToReinstance.Find(ObjectProperty->PropertyClass))
			{
				ObjectProperty->PropertyClass = *Interface;
			}
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
	
	FBlueprintEditorUtils::FOnNodeFoundOrUpdated DummyDelegate = [](UBlueprint* BP, UK2Node* Node)
	{
		
	};

	for (TObjectIterator<UBlueprint> BlueprintIt; BlueprintIt; ++BlueprintIt)
	{
		UBlueprint* BP = *BlueprintIt;
		UClass* OldClass = Cast<UClass>(BP->GeneratedClass);
		
		if (!OldClass)
		{
			continue;
		}

		UpdatePins(OldClass);

		TArray<UK2Node*> AllNodes;
		FBlueprintEditorUtils::GetAllNodesOfClass(BP, AllNodes);
	}
	
	Reload->Reinstance();
	Reload->Finalize(true);
	
	PostReinstance();
	
	auto CleanOldTypes = [](const auto& Container)
	{
		for (const auto& [Old, New] : Container)
		{
			if (!Old || !New)
			{
				continue;
			}

			Old->SetFlags(RF_NewerVersionExists);
			Old->RemoveFromRoot();
		}
	};

	CleanOldTypes(InterfacesToReinstance);
	CleanOldTypes(StructsToReinstance);
	CleanOldTypes(EnumsToReinstance);
	CleanOldTypes(ClassesToReinstance);

	InterfacesToReinstance.Empty();
	StructsToReinstance.Empty();
	EnumsToReinstance.Empty();
	ClassesToReinstance.Empty();
	
	EndReload();
}

void FCSReinstancer::GetDependentBlueprints(TArray<UBlueprint*>& DependentBlueprints)
{
	TArray<UK2Node*> AllNodes;
	TMap<UObject*, UObject*> ClassReplaceList;
	
	for (auto& Elem : ClassesToReinstance)
	{
		ClassReplaceList.Add(Elem.Key, Elem.Value);
	}

	for (auto& Elem : StructsToReinstance)
	{
		ClassReplaceList.Add(Elem.Key, Elem.Value);
	}
	
	auto ReplacePinType = [&](FEdGraphPinType& PinType) -> bool
	{
		if (PinType.PinCategory != UEdGraphSchema_K2::PC_Struct)
		{
			return false;
		}
		
		UScriptStruct* Struct = Cast<UScriptStruct>(PinType.PinSubCategoryObject.Get());
		if (Struct == nullptr)
		{
			return false;
		}
		
		UScriptStruct** NewStruct = StructsToReinstance.Find(Struct);
		if (NewStruct == nullptr)
		{
			return false;
		}
		
		PinType.PinSubCategoryObject = *NewStruct;
		return true;
	};

	for (TObjectIterator<UBlueprint> BlueprintIt; BlueprintIt; ++BlueprintIt)
	{
		UBlueprint* BP = *BlueprintIt;

		AllNodes.Reset();
		FBlueprintEditorUtils::GetAllNodesOfClass(BP, AllNodes);

		bool bHasDependency = false;
		for (UK2Node* Node : AllNodes)
		{
			TArray<UStruct*> Dependencies;
			if (Node->HasExternalDependencies(&Dependencies))
			{
				for (UStruct* Struct : Dependencies)
				{
					if (ClassesToReinstance.Contains(static_cast<UClass*>(Struct)))
					{
						bHasDependency = true;
						break;
					}
					
					if (StructsToReinstance.Contains(static_cast<UScriptStruct*>(Struct)))
					{
						bHasDependency = true;
						break;
					}
				}
			}

			for (auto* Pin : Node->Pins)
			{
				bHasDependency |= ReplacePinType(Pin->PinType);
			}

			if (auto* EditableBase = Cast<UK2Node_EditablePinBase>(Node))
			{
				for (auto Desc : EditableBase->UserDefinedPins)
				{
					bHasDependency |= ReplacePinType(Desc->PinType);
				}
			}

			if (auto* MacroInst = Cast<UK2Node_MacroInstance>(Node))
			{
				bHasDependency |= ReplacePinType(MacroInst->ResolvedWildcardType);
			}
		}

		for (auto& Variable : BP->NewVariables)
		{
			bHasDependency |= ReplacePinType(Variable.VarType);
		}

		// Check if the blueprint references any of our replacing classes at all
		FArchiveReplaceObjectRef ReplaceObjectArch(BP, ClassReplaceList, EArchiveReplaceObjectFlags::IgnoreOuterRef | EArchiveReplaceObjectFlags::IgnoreArchetypeRef);
		if (ReplaceObjectArch.GetCount())
		{
			bHasDependency = true;
		}

		if (bHasDependency)
		{
			DependentBlueprints.Add(BP);
		}
	}
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
