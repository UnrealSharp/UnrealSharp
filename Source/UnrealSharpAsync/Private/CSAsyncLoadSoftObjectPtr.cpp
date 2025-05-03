#include "CSAsyncLoadSoftObjectPtr.h"
#include "Engine/AssetManager.h"
#include "Engine/StreamableManager.h"

void UCSAsyncLoadSoftPtr::LoadSoftObjectPaths(const TArray<FSoftObjectPath>& SoftObjectPtr)
{
	UAssetManager::Get().GetStreamableManager().RequestAsyncLoad(SoftObjectPtr,
	FStreamableDelegate::CreateUObject(this, &UCSAsyncLoadSoftPtr::OnAsyncLoadComplete));
}

void UCSAsyncLoadSoftPtr::OnAsyncLoadComplete()
{
	InvokeManagedCallback();
}

