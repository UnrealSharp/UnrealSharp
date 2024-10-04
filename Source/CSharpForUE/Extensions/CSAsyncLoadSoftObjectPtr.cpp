#include "CSAsyncLoadSoftObjectPtr.h"

#include "CSManager.h"
#include "Engine/AssetManager.h"

void UCSAsyncLoadSoftPtr::Activate()
{
	UAssetManager::Get().GetStreamableManager().RequestAsyncLoad(SoftObjectPtrs,
		FStreamableDelegate::CreateUObject(this, &UCSAsyncLoadSoftPtr::OnAsyncLoadComplete));
}

UCSAsyncLoadSoftObjectPtr* UCSAsyncLoadSoftObjectPtr::AsyncLoadSoftObjectPtr(const TSoftObjectPtr<UObject>& SoftObjectPtr)
{
	UObject* WorldContextObject = UCSManager::Get().GetCurrentWorldContext();
	ensure(WorldContextObject);
	
	UCSAsyncLoadSoftObjectPtr* BlueprintNode = NewObject<UCSAsyncLoadSoftObjectPtr>(WorldContextObject);
	BlueprintNode->SoftObjectPtrs.Add(SoftObjectPtr.ToSoftObjectPath());
	return BlueprintNode;
}

void UCSAsyncLoadSoftObjectPtr::OnAsyncLoadComplete()
{
	UObject* Object = SoftObjectPtrs[0].ResolveObject();
	OnSuccess.Broadcast(Object);
	Super::OnAsyncLoadComplete();
}

UCSAsyncLoadSoftObjectPtrList* UCSAsyncLoadSoftObjectPtrList::AsyncLoadSoftObjectPtrList(const TArray<TSoftObjectPtr<UObject>>& SoftObjectPtr)
{
	UObject* WorldContextObject = UCSManager::Get().GetCurrentWorldContext();
	ensure(WorldContextObject);
	
	UCSAsyncLoadSoftObjectPtrList* BlueprintNode = NewObject<UCSAsyncLoadSoftObjectPtrList>(WorldContextObject);
	
	for (const TSoftObjectPtr<UObject>& SoftObjectPtrItem : SoftObjectPtr)
	{
		BlueprintNode->SoftObjectPtrs.Add(SoftObjectPtrItem.ToSoftObjectPath());
	}
	
	return BlueprintNode;
}

void UCSAsyncLoadSoftObjectPtrList::OnAsyncLoadComplete()
{
	TArray<UObject*> Objects;
	Objects.Reserve(SoftObjectPtrs.Num());
	
	for (const FSoftObjectPath& SoftObjectPtr : SoftObjectPtrs)
	{
		Objects.Add(SoftObjectPtr.ResolveObject());
	}

	OnSuccess.Broadcast(Objects);
	Super::OnAsyncLoadComplete();
}

UCSAsyncLoadSoftClassPtr* UCSAsyncLoadSoftClassPtr::AsyncLoadSoftClassPtr(const TSoftClassPtr<UObject>& SoftObjectPtr)
{
	UObject* WorldContextObject = UCSManager::Get().GetCurrentWorldContext();
	ensure(WorldContextObject);
	
	UCSAsyncLoadSoftClassPtr* BlueprintNode = NewObject<UCSAsyncLoadSoftClassPtr>(WorldContextObject);
	BlueprintNode->SoftObjectPtrs.Add(SoftObjectPtr.ToSoftObjectPath());
	return BlueprintNode;
}

void UCSAsyncLoadSoftClassPtr::OnAsyncLoadComplete()
{
	UObject* Object = SoftObjectPtrs[0].ResolveObject();
	if (IsValid(Object))
	{
		OnSuccess.Broadcast(CastChecked<UClass>(Object));
	}
	
	Super::OnAsyncLoadComplete();
}

UCSAsyncLoadSoftClassPtrList* UCSAsyncLoadSoftClassPtrList::AsyncLoadSoftClassPtrList(const TArray<TSoftClassPtr<UObject>>& SoftObjectPtr)
{
	UObject* WorldContextObject = UCSManager::Get().GetCurrentWorldContext();
	ensure(WorldContextObject);
	
	UCSAsyncLoadSoftClassPtrList* BlueprintNode = NewObject<UCSAsyncLoadSoftClassPtrList>(WorldContextObject);
	
	for (const TSoftClassPtr<UObject>& SoftObjectPtrItem : SoftObjectPtr)
	{
		BlueprintNode->SoftObjectPtrs.Add(SoftObjectPtrItem.ToSoftObjectPath());
	}
	
	return BlueprintNode;
}

void UCSAsyncLoadSoftClassPtrList::OnAsyncLoadComplete()
{
	TArray<TSubclassOf<UObject>> Classes;
	Classes.Reserve(SoftObjectPtrs.Num());
	
	for (const FSoftObjectPath& SoftObjectPtr : SoftObjectPtrs)
	{
		UObject* Object = SoftObjectPtr.ResolveObject();
		if (!IsValid(Object))
		{
			continue;
		}
		
		Classes.Add(CastChecked<UClass>(Object));
	}

	OnSuccess.Broadcast(Classes);
	Super::OnAsyncLoadComplete();
}

