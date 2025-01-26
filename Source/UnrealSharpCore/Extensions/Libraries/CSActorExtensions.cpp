#include "CSActorExtensions.h"
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
	UCSClass* ClassData = FCSGeneratedClassBuilder::GetFirstManagedClass(Actor->GetClass());

	if (!IsValid(ClassData))
	{
		return nullptr;
	}

	USimpleConstructionScript* SCS = ClassData->SimpleConstructionScript;
	USCS_Node* Node = SCS->FindSCSNode(ComponentName);
	return Node->ComponentTemplate;
}
