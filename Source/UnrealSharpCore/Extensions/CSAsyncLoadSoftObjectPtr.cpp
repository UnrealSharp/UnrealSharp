#include "CSAsyncLoadSoftObjectPtr.h"

#include "CSManager.h"
#include "Engine/AssetManager.h"

void UCSAsyncLoadSoftPtr::Activate()
{
	RegisterWithGameInstance(GetOuter());
	
	UAssetManager::Get().GetStreamableManager().RequestAsyncLoad(SoftObjectPtrs,
		FStreamableDelegate::CreateUObject(this, &UCSAsyncLoadSoftPtr::OnAsyncLoadComplete));
}

UCSAsyncLoadSoftObjectPtr* UCSAsyncLoadSoftObjectPtr::AsyncLoadSoftObjectPtr(const FSoftObjectPath& SoftObjectPtr)
{
	UObject* WorldContextObject = UCSManager::Get().GetCurrentWorldContext();
	ensure(WorldContextObject);
	
	UCSAsyncLoadSoftObjectPtr* Action = NewObject<UCSAsyncLoadSoftObjectPtr>(WorldContextObject->GetWorld());
	Action->SoftObjectPtrs.Add(SoftObjectPtr);
	return Action;
}

void UCSAsyncLoadSoftObjectPtr::OnAsyncLoadComplete()
{
	UObject* Object = SoftObjectPtrs[0].ResolveObject();
	OnSuccess.Broadcast(Object);
	Super::OnAsyncLoadComplete();
}

UCSAsyncLoadSoftObjectPtrList* UCSAsyncLoadSoftObjectPtrList::AsyncLoadSoftObjectPtrList(const TArray<FSoftObjectPath>& SoftObjectPtr)
{
	UObject* WorldContextObject = UCSManager::Get().GetCurrentWorldContext();
	ensure(WorldContextObject);
	
	UCSAsyncLoadSoftObjectPtrList* Action = NewObject<UCSAsyncLoadSoftObjectPtrList>(WorldContextObject->GetWorld());
	Action->SoftObjectPtrs = SoftObjectPtr;
	return Action;
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

UCSAsyncLoadSoftClassPtr* UCSAsyncLoadSoftClassPtr::AsyncLoadSoftClassPtr(const FSoftObjectPath& SoftObjectPtr)
{
	UObject* WorldContextObject = UCSManager::Get().GetCurrentWorldContext();
	ensure(WorldContextObject);
	
	UCSAsyncLoadSoftClassPtr* BlueprintNode = NewObject<UCSAsyncLoadSoftClassPtr>(WorldContextObject->GetWorld());
	BlueprintNode->SoftObjectPtrs.Add(SoftObjectPtr);
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

UCSAsyncLoadSoftClassPtrList* UCSAsyncLoadSoftClassPtrList::AsyncLoadSoftClassPtrList(const TArray<FSoftObjectPath>& SoftObjectPtr)
{
	UObject* WorldContextObject = UCSManager::Get().GetCurrentWorldContext();
	ensure(WorldContextObject);
	
	UCSAsyncLoadSoftClassPtrList* Action = NewObject<UCSAsyncLoadSoftClassPtrList>(WorldContextObject->GetWorld());
	Action->SoftObjectPtrs = SoftObjectPtr;
	return Action;
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

