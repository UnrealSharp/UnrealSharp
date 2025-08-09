#pragma once

#include "CoreMinimal.h"
#include "CSAsyncActionBase.h"
#include "CSAsyncLoadSoftObjectPtr.generated.h"

UCLASS(meta = (InternalType))
class UCSAsyncLoadSoftPtr : public UCSAsyncActionBase
{
	GENERATED_BODY()
public:
	UFUNCTION(meta = (ScriptMethod))
	void LoadSoftObjectPaths(const TArray<FSoftObjectPath>& SoftObjectPtr);
protected:
	void OnAsyncLoadComplete();
};




