#include "CSActorExtensions.h"

#include "UnrealSharpCore.h"
#include "Engine/InheritableComponentHandler.h"
#include "Engine/SCS_Node.h"
#include "Engine/SimpleConstructionScript.h"
#include "TypeGenerator/CSClass.h"
#include "TypeGenerator/Register/CSGeneratedClassBuilder.h"
#include "Utils/CSClassUtilities.h"

#ifdef __clang__
#pragma clang diagnostic ignored "-Wextern-initializer"
#endif

namespace ReflectionHelper
{
	extern inline FName Records = FName(TEXT("Records"));
	extern inline FString RootPrefix = TEXT("ICH-");
}

void UCSActorExtensions::AddReplicatedSubObject(AActor* Actor, UObject* SubObject, ELifetimeCondition NetCondition)
{
	Actor->AddReplicatedSubObject(SubObject, NetCondition);
}

void UCSActorExtensions::RemoveReplicatedSubObject(AActor* Actor, UObject* SubObject)
{
	Actor->RemoveReplicatedSubObject(SubObject);
}

bool UCSActorExtensions::IsReplicatedSubObjectRegistered(AActor* Actor, UObject* SubObject)
{
	return Actor->IsReplicatedSubObjectRegistered(SubObject);
}

UActorComponent* UCSActorExtensions::GetComponentTemplate(const AActor* Actor, FName ComponentName)
{
	if (!IsValid(Actor))
	{
		return nullptr;
	}

	UBlueprintGeneratedClass* CurrentClass = FCSClassUtilities::GetFirstManagedClass(Actor->GetClass());
	while (IsValid(CurrentClass))
	{
		if (USimpleConstructionScript* SCS = CurrentClass->SimpleConstructionScript)
		{
			if (USCS_Node* Node = SCS->FindSCSNode(ComponentName))
			{
				return Node->ComponentTemplate;
			}
		}

		// If it's not our component, it's inherited and we need to create a new record for it
		if (UInheritableComponentHandler* InheritableComponentHandler = CurrentClass->GetInheritableComponentHandler(true))
		{
#if WITH_EDITOR
			if (GIsEditor)
			{
				UBlueprint* Blueprint = static_cast<UBlueprint*>(CurrentClass->ClassGeneratedBy);
				Blueprint->InheritableComponentHandler = InheritableComponentHandler;
			}
#endif
			
			FComponentKey ComponentKey = InheritableComponentHandler->FindKey(ComponentName);
			
			if (UActorComponent* ComponentTemplate = InheritableComponentHandler->GetOverridenComponentTemplate(ComponentKey))
			{
				return ComponentTemplate;
			}
			
			USCS_Node* OriginalTemplate = nullptr;
			for (UBlueprintGeneratedClass* SCSClass = CurrentClass; SCSClass; SCSClass = Cast<UBlueprintGeneratedClass>(SCSClass->GetSuperClass()))
			{
				if (USimpleConstructionScript* SCS = SCSClass->SimpleConstructionScript)
				{
					OriginalTemplate = SCS->FindSCSNode(ComponentName);
					if (OriginalTemplate)
					{
						break;
					}
				}
			}
				
			if (OriginalTemplate)
			{
				static UClass* HandlerClass = UInheritableComponentHandler::StaticClass();
				static FArrayProperty* RecordsArray = FindFieldChecked<FArrayProperty>(HandlerClass, ReflectionHelper::Records);

				FScriptArrayHelper_InContainer ArrayHelper(RecordsArray, InheritableComponentHandler);
				int32 NewIndex = ArrayHelper.AddValue();
				FComponentOverrideRecord* NewRecord = reinterpret_cast<FComponentOverrideRecord*>(ArrayHelper.GetRawPtr(NewIndex));

				CreateNewRecord(InheritableComponentHandler, FComponentKey(OriginalTemplate), NewRecord);
				return NewRecord->ComponentTemplate;
			}
		}
		
		CurrentClass = Cast<UBlueprintGeneratedClass>(CurrentClass->GetSuperClass());
	}

	UE_LOG(LogUnrealSharp, Error, TEXT("Component %s not found in actor %s. Should not happen to DefaultComponents"), *ComponentName.ToString(), *Actor->GetName());
	return nullptr;
}

FBox UCSActorExtensions::GetComponentsBoundingBox(const AActor* Actor, bool bNonColliding, bool bIncludeFromChildActors)
{
	return Actor->GetComponentsBoundingBox(bNonColliding, bIncludeFromChildActors);
}

void UCSActorExtensions::CreateNewRecord(const UInheritableComponentHandler* InheritableComponentHandler, const FComponentKey& Key, FComponentOverrideRecord* NewRecord)
{
	UActorComponent* BestArchetype = FindBestArchetype(InheritableComponentHandler->GetOuter(), Key);
	FName NewComponentTemplateName = BestArchetype->GetFName();
	
	if (USCS_Node* SCSNode = Key.FindSCSNode())
	{
		const USCS_Node* DefaultSceneRootNode = SCSNode->GetSCS()->GetDefaultSceneRootNode();
		if (SCSNode == DefaultSceneRootNode && BestArchetype == DefaultSceneRootNode->ComponentTemplate)
		{
			NewComponentTemplateName = *(ReflectionHelper::RootPrefix + BestArchetype->GetName());
		}
	}
		
	UObject* Outer = InheritableComponentHandler->GetOuter();
	constexpr EObjectFlags Flags = RF_ArchetypeObject | RF_Public | RF_InheritableComponentTemplate;
	UActorComponent* NewComponentTemplate = NewObject<UActorComponent>(Outer, BestArchetype->GetClass(), NewComponentTemplateName, Flags, BestArchetype);
	
	NewRecord->ComponentKey = Key;
	NewRecord->ComponentClass = NewComponentTemplate->GetClass();
	NewRecord->ComponentTemplate = NewComponentTemplate;
}

UActorComponent* UCSActorExtensions::FindBestArchetype(UObject* Outer, FComponentKey Key, FName TemplateName)
{
	UActorComponent* ClosestArchetype = nullptr;
	UBlueprintGeneratedClass* MainClass = Cast<UBlueprintGeneratedClass>(Outer);
	
	if (MainClass && Key.GetComponentOwner() && MainClass != Key.GetComponentOwner())
	{
		while (!ClosestArchetype && MainClass)
		{
			if (MainClass->InheritableComponentHandler)
			{
				ClosestArchetype = MainClass->InheritableComponentHandler->GetOverridenComponentTemplate(Key);
			}
			
			MainClass = Cast<UBlueprintGeneratedClass>(MainClass->GetSuperClass());
		}

		if (!ClosestArchetype)
		{
			ClosestArchetype = Key.GetOriginalTemplate(TemplateName);
		}
	}

	return ClosestArchetype;
}
