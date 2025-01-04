#include "CSActorExtensions.h"

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
