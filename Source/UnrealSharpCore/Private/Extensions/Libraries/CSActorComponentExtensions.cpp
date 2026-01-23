#include "Extensions/Libraries/CSActorComponentExtensions.h"

void UCSActorComponentExtensions::AddReplicatedSubObject(UActorComponent* ActorComponent, UObject* SubObject, ELifetimeCondition NetCondition)
{
	ActorComponent->AddReplicatedSubObject(SubObject, NetCondition);
}

void UCSActorComponentExtensions::RemoveReplicatedSubObject(UActorComponent* ActorComponent, UObject* SubObject)
{
	ActorComponent->RemoveReplicatedSubObject(SubObject);
}

bool UCSActorComponentExtensions::IsReplicatedSubObjectRegistered(UActorComponent* ActorComponent, UObject* SubObject)
{
	return ActorComponent->IsReplicatedSubObjectRegistered(SubObject);
}

void UCSActorComponentExtensions::SetIsReplicated(UActorComponent* ActorComponent, bool bInIsReplicated)
{
	static FBoolProperty* ReplicatesProperty = FindFieldChecked<FBoolProperty>(UActorComponent::StaticClass(), TEXT("bReplicates"));
	
	if (ActorComponent->HasAnyFlags(RF_NeedInitialization))
	{
		ReplicatesProperty->SetPropertyValue_InContainer(ActorComponent, bInIsReplicated);
	}
	else
	{
		ActorComponent->SetIsReplicated(bInIsReplicated);
	}
}

bool UCSActorComponentExtensions::GetIsReplicated(const UActorComponent* ActorComponent)
{
	return ActorComponent->GetIsReplicated();
}
