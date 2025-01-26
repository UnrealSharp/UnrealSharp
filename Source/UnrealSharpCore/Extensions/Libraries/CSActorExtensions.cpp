#include "CSActorExtensions.h"

#include "Engine/InheritableComponentHandler.h"
#include "Engine/SCS_Node.h"
#include "Engine/SimpleConstructionScript.h"
#include "TypeGenerator/Register/CSGeneratedClassBuilder.h"

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

	UBlueprintGeneratedClass* CurrentClass = Cast<UBlueprintGeneratedClass>(Actor->GetClass());

	while (CurrentClass)
	{
		if (USimpleConstructionScript* SCS = CurrentClass->SimpleConstructionScript)
		{
			if (USCS_Node* Node = SCS->FindSCSNode(ComponentName))
			{
				return Node->ComponentTemplate;
			}
		}

		if (UInheritableComponentHandler* InheritableComponentHandler = CurrentClass->GetInheritableComponentHandler())
		{
			FComponentKey ComponentKey = InheritableComponentHandler->FindKey(ComponentName);
			if (UActorComponent* ComponentTemplate = InheritableComponentHandler->GetOverridenComponentTemplate(ComponentKey))
			{
				return ComponentTemplate;
			}
		}
		
		CurrentClass = Cast<UBlueprintGeneratedClass>(CurrentClass->GetSuperClass());
	}

	return nullptr;
}
