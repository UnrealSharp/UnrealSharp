// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "Engine/CancellableAsyncAction.h"
#include "CSCancellableAsyncAction.generated.h"

UCLASS(Blueprintable, BlueprintType, Abstract)
class CSHARPFORUE_API UCSCancellableAsyncAction : public UCancellableAsyncAction
{
	GENERATED_BODY()

public:

	// Start UCancellableAsyncAction Functions
	virtual void Activate() override;
	virtual void Cancel() override;
	// End UCancellableAsyncAction Functions

protected:

	UFUNCTION(BlueprintImplementableEvent, meta = (DisplayName = "Activate"))
	void ReceiveActivate();

	UFUNCTION(BlueprintImplementableEvent, meta = (DisplayName = "Cancel"))
	void ReceiveCancel();

};
