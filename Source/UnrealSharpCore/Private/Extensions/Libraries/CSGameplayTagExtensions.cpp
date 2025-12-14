#include "Extensions/Libraries/CSGameplayTagExtensions.h"
#include "GameplayTagContainer.h"

bool UCSGameplayTagExtensions::MatchesTag(const FGameplayTag& Tag, const FGameplayTag& Other)
{
	return Tag.MatchesTag(Other);
}

int32 UCSGameplayTagExtensions::MatchesTagDepth(const FGameplayTag& Tag, const FGameplayTag& Other)
{
	return Tag.MatchesTagDepth(Other);
}

bool UCSGameplayTagExtensions::MatchesAny(const FGameplayTag& Tag, const FGameplayTagContainer& Tags)
{
	return Tag.MatchesAny(Tags);
}

bool UCSGameplayTagExtensions::MatchesAnyExact(const FGameplayTag& Tag, const FGameplayTagContainer& Tags)
{
	return Tag.MatchesAnyExact(Tags);
}

FGameplayTag UCSGameplayTagExtensions::RequestGameplayTag(const FName TagName)
{
	return FGameplayTag::RequestGameplayTag(TagName);
}

FName UCSGameplayTagExtensions::GetTagLeafName(const FGameplayTag Tag)
{
	#if ENGINE_MAJOR_VERSION >= 5 && ENGINE_MINOR_VERSION >= 5
		return Tag.GetTagLeafName();
	#else
		return NAME_None;
	#endif
}

FGameplayTag UCSGameplayTagExtensions::RequestDirectParent(const FGameplayTag Tag)
{
    return Tag.RequestDirectParent();
}

FGameplayTagContainer UCSGameplayTagExtensions::GetGameplayTagParents(const FGameplayTag Tag)
{
    return Tag.GetGameplayTagParents();
}

FGameplayTagContainer UCSGameplayTagExtensions::GetSingleTagContainer(const FGameplayTag Tag)
{
    return Tag.GetSingleTagContainer();
}
