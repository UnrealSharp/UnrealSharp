#include "CSReplicatedObject.h"
#include "Engine/BlueprintGeneratedClass.h"
#include "Runtime/Launch/Resources/Version.h"
#include "UObject/Package.h"
#include "Engine/NetDriver.h"
#include "Engine/Engine.h"

UWorld* UCSReplicatedObject::GetWorld() const
{
	if (GetOuter() == nullptr)
	{
		return nullptr;
	}
		
	if (Cast<UPackage>(GetOuter()) != nullptr)
	{
		return Cast<UWorld>(GetOuter()->GetOuter());
	}
		
	return GetOwningActor()->GetWorld();
}

void UCSReplicatedObject::GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const
{
	Super::GetLifetimeReplicatedProps(OutLifetimeProps);
	
	if (UBlueprintGeneratedClass* BPCClass = Cast<UBlueprintGeneratedClass>(GetClass()))
	{
		BPCClass->GetLifetimeBlueprintReplicationList(OutLifetimeProps);
	}
}

bool UCSReplicatedObject::IsSupportedForNetworking() const
{
	return ReplicationState == ECSReplicationState::Replicates;
}

int32 UCSReplicatedObject::GetFunctionCallspace(UFunction* Function, FFrame* Stack)
{
	if (HasAnyFlags(RF_ClassDefaultObject) || !IsSupportedForNetworking())
	{
		return GEngine->GetGlobalFunctionCallspace(Function, this, Stack);
	}
	
	return GetOuter()->GetFunctionCallspace(Function, Stack);
}

bool UCSReplicatedObject::CallRemoteFunction(UFunction* Function, void* Parms, FOutParmRec* OutParms, FFrame* Stack)
{
	AActor* Owner = GetOwningActor();
	UNetDriver* NetDriver = Owner->GetNetDriver();
	if (!IsValid(NetDriver))
	{
		return false;
	}
	
	NetDriver->ProcessRemoteFunction(Owner, Function, Parms, OutParms, Stack, this);
	return true;
}

AActor* UCSReplicatedObject::GetOwningActor() const
{
	return GetTypedOuter<AActor>();
}

void UCSReplicatedObject::DestroyObject()
{
	if (!IsValid(this))
	{
		return;
	}

	MarkAsGarbage();
}
