#pragma once

#include "CoreMinimal.h"
#include "K2Node_CSAsyncAction.h"
#include "UObject/ObjectMacros.h"
#include "UObject/UObjectGlobals.h"

#include "K2Node_CSCancellableAsyncAction.generated.h"

UCLASS()
class UK2Node_CSCancellableAsyncAction : public UK2Node_CSAsyncAction
{
	GENERATED_BODY()
public:
	UK2Node_CSCancellableAsyncAction();
};
