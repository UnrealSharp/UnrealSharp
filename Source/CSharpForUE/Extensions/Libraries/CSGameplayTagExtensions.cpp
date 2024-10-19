// Fill out your copyright notice in the Description page of Project Settings.


#include "CSGameplayTagExtensions.h"

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
