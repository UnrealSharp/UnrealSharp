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

	const TObjectPtr<UClass> & GetProxyClass() const
	{
		return ProxyClass;
	};

	const FName & GetFactoryFunctionName() const
	{
		return ProxyFactoryFunctionName;
	}

	// UK2Node interface
	virtual void GetMenuActions(FBlueprintActionDatabaseRegistrar& ActionRegistrar) const override;
	// End of UK2Node interface
};
