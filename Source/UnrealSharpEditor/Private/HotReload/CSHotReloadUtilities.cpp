#include "HotReload/CSHotReloadUtilities.h"

#include "CSManager.h"
#include "CSUnrealSharpEditorSettings.h"
#include "IDirectoryWatcher.h"
#include "IPlacementModeModule.h"
#include "Projects.h"
#include "Engine/InheritableComponentHandler.h"
#include "Engine/SCS_Node.h"
#include "Engine/SimpleConstructionScript.h"
#include "Kismet2/DebuggerCommands.h"
#include "Kismet2/KismetEditorUtilities.h"
#include "Utilities/CSAssemblyUtilities.h"
#include "Utilities/CSClassUtilities.h"

void FCSHotReloadUtilities::SortDirtiedFiles(TArray<FCSChangedFile>& Files)
{
	return Files.Sort([](const FCSChangedFile& A, const FCSChangedFile& B)
	{
		return A.ChangeData.Action == FFileChangeData::FCA_Removed &&
			B.ChangeData.Action != FFileChangeData::FCA_Removed;
	});
}

bool FCSHotReloadUtilities::HasFileBeenDirtied(const TArray<FCSChangedFile>& DirtiedFiles, const FString& FilePath, FFileChangeData::EFileChangeAction Action)
{
	bool bFound = false;
	for (const FCSChangedFile& DirtiedFile : DirtiedFiles)
	{
		if (DirtiedFile.FilePath != FilePath || DirtiedFile.ChangeData.Action != Action)
		{
			continue;
		}
		
		bFound = true;
		break;
	}
	
	return bFound;
}

void FCSHotReloadUtilities::CollectDirtiedFiles(const TArray<FFileChangeData>& ChangedFiles, TArray<FCSChangedFile>& OutDirtied)
{
	for (const FFileChangeData& Change : ChangedFiles)
	{
		FString NormalizedPath = Change.Filename;
		NormalizedPath.ReplaceInline(TEXT("/"), TEXT("\\"));
		
		if (HasFileBeenDirtied(OutDirtied, NormalizedPath, Change.Action))
		{
			continue;
		}
		
		FCSChangedFile ChangedFile = FCSChangedFile(NormalizedPath, Change);
		OutDirtied.Add(ChangedFile);
	}
	
	SortDirtiedFiles(OutDirtied);
}

bool FCSHotReloadUtilities::ApplyDirtiedFiles(const FString& ProjectName, const TArray<FCSChangedFile>& DirtyFiles, FString& OutException)
{
	FUnrealSharpEditorModule& EditorModule = FUnrealSharpEditorModule::Get();
	const FCSManagedEditorCallbacks& Callbacks = EditorModule.GetManagedEditorCallbacks();
	
	for (const FCSChangedFile& DirtyFile : DirtyFiles)
	{
		if (DirtyFile.ChangeData.Action == FFileChangeData::FCA_Removed)
		{
			Callbacks.RemoveSourceFile(*ProjectName, *DirtyFile.FilePath);
		}
		else
		{
			Callbacks.RecompileChangedFile(*ProjectName, *DirtyFile.FilePath, &OutException);
			
			if (!OutException.IsEmpty())
			{
				return false;
			}
		}
	}
	
	return true;
}

bool FCSHotReloadUtilities::RecompileDirtyProjects(const TArray<UCSManagedAssembly*>& Assemblies, FString& OutExceptionMessage)
{
	FUnrealSharpEditorModule& UnrealSharpEditorModule = FUnrealSharpEditorModule::Get();
	
	TArray<FString> AssemblyNames;
	AssemblyNames.Reserve(Assemblies.Num());
	
	for (UCSManagedAssembly* Assembly : Assemblies)
	{
		if (!IsValid(Assembly))
		{
			continue;
		}
		
		AssemblyNames.Add(Assembly->GetAssemblyName().ToString());
	}
	
	return UnrealSharpEditorModule.GetManagedEditorCallbacks().RecompileDirtyProjects(&OutExceptionMessage, AssemblyNames);
}

void FCSHotReloadUtilities::RebuildDependentBlueprints(const TSet<uint32>& RebuiltTypes)
{
	TRACE_CPUPROFILER_EVENT_SCOPE(FCSHotReloadUtilities::RebuildDependentBlueprints);

	if (RebuiltTypes.IsEmpty())
	{
		return;
	}
	
	for (TObjectIterator<UBlueprint> BlueprintIt; BlueprintIt; ++BlueprintIt)
	{
		UBlueprint* Blueprint = *BlueprintIt;
		UClass* GeneratedClass = Blueprint->GeneratedClass;
		
		if (!IsValid(GeneratedClass) || FCSClassUtilities::IsManagedClass(GeneratedClass))
		{
			continue;
		}
		
		TArray<UEdGraph*> Graphs;
		Blueprint->GetAllGraphs(Graphs);
		
		bool bNeedsRecompile = false;
		for (const UEdGraph* Graph : Graphs)
		{
			for (const TObjectPtr Node : Graph->Nodes)
			{
				if (!IsNodeAffectedByReload(Node, RebuiltTypes))
				{
					continue;
				}

				Node->ReconstructNode();
				bNeedsRecompile = true;
			}
		}

		if (!bNeedsRecompile)
		{
			for (const FBPVariableDescription& VarDesc : Blueprint->NewVariables)
			{
				if (!IsPinAffectedByReload(VarDesc.VarType, RebuiltTypes))
				{
					continue;
				}
				
				bNeedsRecompile = true;
				break;
			}
		}

		if (!bNeedsRecompile)
		{
			UClass* ParentClass = Blueprint->ParentClass;
			while (IsValid(ParentClass))
			{
				if (UCSManager::Get().IsManagedType(ParentClass))
				{
					int32 TypeID = ParentClass->GetUniqueID();
					
					if (RebuiltTypes.ContainsByHash(TypeID, TypeID))
					{
						bNeedsRecompile = true;
						break;
					}
				}

				ParentClass = ParentClass->GetSuperClass();
			}
		}
		
		if (!bNeedsRecompile && GeneratedClass->IsChildOf(AActor::StaticClass()))
		{
			bNeedsRecompile = HasDefaultComponentsBeenAffected(Blueprint, RebuiltTypes);
		}
		
		if (!bNeedsRecompile)
		{
			continue;
		}

		FKismetEditorUtilities::CompileBlueprint(Blueprint, EBlueprintCompileOptions::SkipGarbageCollection);
	}
}

void FCSHotReloadUtilities::RefreshPlacementMode()
{
	IPlacementModeModule::Get().OnAllPlaceableAssetsChanged().Broadcast();
	IPlacementModeModule::Get().OnPlaceableItemFilteringChanged().Broadcast();
}

bool FCSHotReloadUtilities::IsPinAffectedByReload(const FEdGraphPinType& PinType, const TSet<uint32>& RebuiltTypes)
{
	UObject* PinSubCategoryObject = PinType.PinSubCategoryObject.Get();
	
	if (!IsValid(PinSubCategoryObject))
	{
		return false;
	}

	uint32 TypeID = PinSubCategoryObject->GetUniqueID();
	if (RebuiltTypes.ContainsByHash(TypeID, TypeID))
	{
		return true;
	}

	UObject* TerminalSubCategoryObject = PinType.PinValueType.TerminalSubCategoryObject.Get();
	if (!IsValid(TerminalSubCategoryObject))
	{
		return false;
	}

	uint32 TerminalTypeID = TerminalSubCategoryObject->GetUniqueID();
	return RebuiltTypes.ContainsByHash(TerminalTypeID, TerminalTypeID);
}

bool FCSHotReloadUtilities::IsNodeAffectedByReload(const UEdGraphNode* Node, const TSet<uint32>& RebuiltTypes)
{
	if (const UK2Node_EditablePinBase* EditableNode = Cast<UK2Node_EditablePinBase>(Node))
	{
		for (const TSharedPtr<FUserPinInfo>& Pin : EditableNode->UserDefinedPins)
		{
			if (!IsPinAffectedByReload(Pin->PinType, RebuiltTypes))
			{
				continue;
			}
			
			return true;
		}
	}

	for (UEdGraphPin* Pin : Node->Pins)
	{
		if (!IsPinAffectedByReload(Pin->PinType, RebuiltTypes))
		{
			continue;
		}
		
		return true;
	}

	return false;
}

bool FCSHotReloadUtilities::HasDefaultComponentsBeenAffected(const UBlueprint* Blueprint, const TSet<uint32>& RebuiltTypes)
{
	auto CheckIfTemplateIsAffected = [&](const UClass* TemplateClass) -> bool
	{
		if (!IsValid(TemplateClass))
		{
			return false;
		}

		int32 TypeID = TemplateClass->GetUniqueID();
		return RebuiltTypes.ContainsByHash(TypeID, TypeID);
	};

	USimpleConstructionScript* SCS = Blueprint->SimpleConstructionScript;
	if (IsValid(SCS))
	{
		for (const USCS_Node* Node : SCS->GetAllNodes())
		{
			if (!CheckIfTemplateIsAffected(Node->ComponentClass))
			{
				continue;
			}

			return true;
		}
	}

	UInheritableComponentHandler* ComponentHandler = Blueprint->InheritableComponentHandler;
	if (IsValid(ComponentHandler))
	{
		TArray<UActorComponent*> OutArray;
		ComponentHandler->GetAllTemplates(OutArray);

		for (UActorComponent* ComponentTemplate : OutArray)
		{
			if (!CheckIfTemplateIsAffected(ComponentTemplate->GetClass()))
			{
				continue;
			}

			return true;
		}
	}

	return false;
}

void FCSHotReloadUtilities::GetChangedCSharpFiles(const TArray<FFileChangeData>& ChangedFiles, TArray<FFileChangeData>& OutFilteredFiles)
{
	OutFilteredFiles.Empty(ChangedFiles.Num());
	
	for (const FFileChangeData& ChangedFile : ChangedFiles)
	{
		if (IsSkippablePath(ChangedFile.Filename))
		{
			continue;
		}
		
		OutFilteredFiles.Add(ChangedFile);
	}
}

bool FCSHotReloadUtilities::ShouldDeferHotReloadRequest(const UCSManagedAssembly* ModifiedAssembly)
{
	if (FCSAssemblyUtilities::IsGlueAssembly(ModifiedAssembly))
	{
		return true;
	}
	
	const UCSUnrealSharpEditorSettings* Settings = GetDefault<UCSUnrealSharpEditorSettings>();
	if (Settings->AutomaticHotReloading == OnEditorFocus || Settings->AutomaticHotReloading == Off)
	{
		return true;
	}
	
	return false;
}

bool FCSHotReloadUtilities::ShouldHotReloadOnEditorFocus(const UCSHotReloadSubsystem* HotReloadSubsystem)
{
	const UCSUnrealSharpEditorSettings* Settings = GetDefault<UCSUnrealSharpEditorSettings>();
	if (Settings->AutomaticHotReloading != OnEditorFocus)
	{
		return false;
	}
	
	if (!HotReloadSubsystem->HasPendingHotReloadChanges())
	{
		return false;
	}
	
	if (!FApp::HasFocus())
	{
		return false;
	}
	
	return true;
}
