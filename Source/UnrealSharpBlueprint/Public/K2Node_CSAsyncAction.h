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
class UNREALSHARPBLUEPRINT_API UK2Node_CSAsyncAction : public UK2Node_BaseAsyncTask
{
	GENERATED_BODY()

public:

	UK2Node_CSAsyncAction();

	static void SetNodeFunc(UEdGraphNode* NewNode, bool, TWeakObjectPtr<UFunction> FunctionPtr);

	// UK2Node interface
	virtual void GetMenuActions(FBlueprintActionDatabaseRegistrar& ActionRegistrar) const override;
	virtual void ExpandNode(class FKismetCompilerContext& CompilerContext, UEdGraph* SourceGraph) override;
	// End of UK2Node interface
};
