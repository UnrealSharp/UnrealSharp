#pragma once

#include "CoreMinimal.h"
#include "UObject/Object.h"
#include "CSReplicatedObject.generated.h"

UENUM(BlueprintType)
enum ECSReplicationState
{
	// This UObject is considered for replication.
	Replicates,
	// This UObject is not considered for replication.
	DoNotReplicate,
};

/** 
 * This class provides support for replicated UObjects in C#
 */
UCLASS(Blueprintable, BlueprintType, DisplayName = "Replicated UObject", Abstract)
class UCSReplicatedObject : public UObject
{
	GENERATED_BODY()

public:

	// UObject interface implementation
	virtual UWorld* GetWorld() const override;
	virtual void GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const override;
	virtual bool IsSupportedForNetworking() const override;
	virtual int32 GetFunctionCallspace(UFunction* Function, FFrame* Stack) override;
	virtual bool CallRemoteFunction(UFunction* Function, void* Parms, struct FOutParmRec* OutParms, FFrame* Stack) override;
	// End of implementation

	// Will mark this UObject as garbage and will eventually get cleaned by the garbage collector.
	// Should only execute this on the server.
	UFUNCTION(meta = (ScriptMethod))
	void DestroyObject();

	// Gets the Actor that "owns" this Replicated UObject.
	UFUNCTION(meta = (ScriptMethod))
	AActor* GetOwningActor() const;

public:

	// Is this UObject replicated?
	UPROPERTY(EditAnywhere)
	TEnumAsByte<ECSReplicationState> ReplicationState = ECSReplicationState::Replicates;
};