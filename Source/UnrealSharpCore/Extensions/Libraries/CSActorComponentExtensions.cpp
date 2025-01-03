#include "CSActorComponentExtensions.h"

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
