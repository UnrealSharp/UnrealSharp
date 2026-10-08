#include "Types/CSClass.h"

#include "CSManagedAssembly.h"
#include "UnrealSharpCore.h"
#include "Utilities/CSClassUtilities.h"

#if WITH_EDITOR
#include "Engine/SCS_Node.h"
#include "Engine/SimpleConstructionScript.h"
#endif

void UCSClass::ManagedObjectConstructor(const FObjectInitializer& ObjectInitializer)
{
	UObject* Object = ObjectInitializer.GetObj();
	
	UCSClass* FirstManagedClass = FCSClassUtilities::GetFirstManagedClass(Object->GetClass());
	const UClass* FirstNativeClass = FCSClassUtilities::GetFirstNativeClass(FirstManagedClass);
	
	// Execute the native class' constructor first.
	FirstNativeClass->ClassConstructor(ObjectInitializer);

	// Initialize managed properties that are not zero initialized such as FText.
	for (UClass* ClassItr = FirstManagedClass; ClassItr != nullptr; ClassItr = ClassItr->GetSuperClass())
	{
		if (!FCSClassUtilities::IsManagedClass(ClassItr))
		{
			break;
		}
		
		for (TFieldIterator<FProperty> PropertyIt(ClassItr, EFieldIterationFlags::None); PropertyIt; ++PropertyIt)
		{
			FProperty* Property = *PropertyIt;
		
			if (Property->HasAnyPropertyFlags(CPF_ZeroConstructor))
			{
				continue;
			}

			Property->InitializeValue_InContainer(Object);
		}
	}
	
	if (FirstManagedClass->IsCreationDeferred())
	{
		return;
	}
	
	TSharedPtr<FCSManagedTypeDefinition> ManagedTypeDefinition = FirstManagedClass->GetManagedTypeDefinition();
	UCSManagedAssembly* OwningAssembly = ManagedTypeDefinition->GetOwningAssembly();
	
	OwningAssembly->CreateManagedObjectFromNative(Object, ManagedTypeDefinition->GetTypeGCHandle());
}

#if WITH_EDITOR
UObject* UCSClass::FindArchetype(const UClass* ArchetypeClass, const FName ArchetypeName) const
{
	UObject* Archetype = Super::FindArchetype(ArchetypeClass, ArchetypeName);
	static const FName DefaultSceneRootName(TEXT("DefaultSceneRoot_GEN_VARIABLE"));
	if (ArchetypeName != DefaultSceneRootName || !SimpleConstructionScript)
	{
		return Archetype;
	}

	const USCS_Node* DefaultSceneRoot = SimpleConstructionScript->GetDefaultSceneRootNode();
	if (!DefaultSceneRoot || Archetype != DefaultSceneRoot->ComponentTemplate || !Archetype || Archetype->GetOuter() != this)
	{
		return Archetype;
	}

	// ValidateSceneRootNodes retains an editor placeholder after a real root replaces it.
	// Do not serialize that placeholder as a child's archetype: it is absent in cooked managed classes.
	TArray<USCS_Node*> Nodes = SimpleConstructionScript->GetRootNodes();
	TSet<const USCS_Node*> Visited;
	for (int32 NodeIndex = 0; NodeIndex < Nodes.Num(); ++NodeIndex)
	{
		const USCS_Node* Node = Nodes[NodeIndex];
		if (!Node || Visited.Contains(Node))
		{
			continue;
		}
		Visited.Add(Node);
		if (Node->ComponentTemplate == Archetype)
		{
			return Archetype;
		}
		Nodes.Append(Node->GetChildNodes());
	}

	const UClass* ParentClass = GetSuperClass();
	return ParentClass ? ParentClass->FindArchetype(ArchetypeClass, ArchetypeName) : nullptr;
}

void UCSClass::PostDuplicate(bool bDuplicateForPIE)
{
	Super::PostDuplicate(bDuplicateForPIE);
	
	UBlueprint* Blueprint = Cast<UBlueprint>(ClassGeneratedBy);
	if (!IsValid(Blueprint))
	{
		UE_LOG(LogUnrealSharp, Error, TEXT("PostDuplicate called on a class without a valid Blueprint: %s"), *GetName());
		return;
	}
	
	UCSClass* ManagedClass = Cast<UCSClass>(Blueprint->GeneratedClass);
	if (!IsValid(ManagedClass))
	{
		UE_LOG(LogUnrealSharp, Error, TEXT("PostDuplicate called on a class that is not a UCSClass: %s"), *GetName());
		return;
	}
	
	SetManagedTypeDefinition(ManagedClass->GetManagedTypeDefinition());
}

void UCSClass::PurgeClass(bool bRecompilingOnLoad)
{
	Super::PurgeClass(bRecompilingOnLoad);
	NumReplicatedProperties = 0;
}
#endif
