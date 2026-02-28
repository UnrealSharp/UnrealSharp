#include "Extensions/GameplayTags/CSGameplayTagsManager.h"
#include "CSManagedAssembly.h"
#include "CSManager.h"
#include "GameplayTagsManager.h"

UCSGameplayTagsManager* UCSGameplayTagsManager::Instance = nullptr;

UCSGameplayTagsManager& UCSGameplayTagsManager::Get()
{
	check(IsInGameThread());

	if (!IsValid(Instance))
	{
		Instance = NewObject<UCSGameplayTagsManager>();
		Instance->AddToRoot();

#if WITH_EDITOR
		UCSManager::Get().OnManagedAssemblyUnloadedEvent().AddUObject(Instance, &UCSGameplayTagsManager::OnManagedAssemblyUnloaded);
#endif
	}

	return *Instance;
}

FGameplayTag UCSGameplayTagsManager::AddTag_Runtime(const FString& TagName, const FString& DevComment)
{
#if !WITH_EDITOR
	FGameplayTag ExistingTag;
	if (IsTagRegistered(TagName, ExistingTag))
	{
		return ExistingTag;
	}

	TSharedPtr<FNativeGameplayTag> NewTag = CreateTag(TagName, DevComment);

	Get().RegisteredTags.Add(NewTag);

	return NewTag->GetTag();
#else
	UE_LOGFMT(LogUnrealSharp, Error, "RegisterTag called in Editor. Use RegisterTag_Editor instead.");
	return FGameplayTag::EmptyTag;
#endif
}

FGameplayTag UCSGameplayTagsManager::AddTag_Editor(const FString& InAssemblyName, const FString& TagName, const FString& DevComment)
{
#if WITH_EDITOR
	FGameplayTag ExistingTag;
	if (IsTagRegistered(TagName, ExistingTag))
	{
		return ExistingTag;
	}

	UCSManagedAssembly* OwningAssembly = UCSManager::Get().FindAssembly(*InAssemblyName);
	if (!IsValid(OwningAssembly))
	{
		UE_LOGFMT(LogUnrealSharp, Error, "Failed to register tag '{0}': Assembly '{1}' not found.", *TagName, *InAssemblyName);
		return FGameplayTag::EmptyTag;
	}
	
	TSharedPtr<FNativeGameplayTag> NewTag = CreateTag(TagName, DevComment);
	
	TArray<TSharedPtr<FNativeGameplayTag>>& Tags = Get().RegisteredTags.FindOrAdd(OwningAssembly->GetAssemblyName());
	Tags.Add(NewTag);
	
	return NewTag->GetTag();
#else
	return AddTag_Runtime(TagName, DevComment);
#endif
}

TSharedPtr<FNativeGameplayTag> UCSGameplayTagsManager::CreateTag(const FString& TagName, const FString& DevComment)
{
	return MakeShared<FNativeGameplayTag>(
		UE_PLUGIN_NAME,
		UE_MODULE_NAME,
		*TagName,
		*DevComment,
		ENativeGameplayTagToken::PRIVATE_USE_MACRO_INSTEAD
	);
}

bool UCSGameplayTagsManager::IsTagRegistered(const FString& TagName, FGameplayTag& OutExistingTag)
{
	if (TagName.IsEmpty())
	{
		return false;
	}

	UGameplayTagsManager& TagManager = UGameplayTagsManager::Get();
	TSharedPtr<FGameplayTagNode> FoundTag = TagManager.FindTagNode(*TagName);

	if (FoundTag.IsValid())
	{
		OutExistingTag = FoundTag->GetCompleteTag();
		return true;
	}

	return false;
}

#if WITH_EDITOR
void UCSGameplayTagsManager::OnManagedAssemblyUnloaded(const UCSManagedAssembly* UnloadedAssembly)
{
	if (!IsValid(UnloadedAssembly))
	{
		return;
	}

	FName AssemblyName = UnloadedAssembly->GetAssemblyName();
	RegisteredTags.Remove(AssemblyName);
}
#endif