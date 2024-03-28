// Copyright Epic Games, Inc. All Rights Reserved.

#pragma once

#include "CoreMinimal.h"
#include "K2Node_BaseAsyncTask.h"
#include "UObject/ObjectMacros.h"
#include "UObject/UObjectGlobals.h"

#include "K2Node_CSAsyncAction.generated.h"

class FBlueprintActionDatabaseRegistrar;
class UObject;

UCLASS()
class UK2Node_CSAsyncAction : public UK2Node_BaseAsyncTask
{
	GENERATED_BODY()

public:

	UK2Node_CSAsyncAction();
	
	// UK2Node interface
	virtual void GetMenuActions(FBlueprintActionDatabaseRegistrar& ActionRegistrar) const override;
	// End of UK2Node interface
};
