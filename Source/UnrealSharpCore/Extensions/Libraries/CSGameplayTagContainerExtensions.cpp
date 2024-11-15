// Fill out your copyright notice in the Description page of Project Settings.


#include "CSGameplayTagContainerExtensions.h"

#include "GameplayTagContainer.h"

bool UCSGameplayTagContainerExtensions::HasTag(const FGameplayTagContainer& Container, const FGameplayTag& Tag)
{
	return Container.HasTag(Tag);
}

bool UCSGameplayTagContainerExtensions::HasTagExact(const FGameplayTagContainer& Container, const FGameplayTag& Tag)
{
	return Container.HasTagExact(Tag);
}

bool UCSGameplayTagContainerExtensions::HasAny(const FGameplayTagContainer& Container, const FGameplayTagContainer& Tags)
{
	return Container.HasAny(Tags);
}

bool UCSGameplayTagContainerExtensions::HasAnyExact(const FGameplayTagContainer& Container, const FGameplayTagContainer& Tags)
{
	return Container.HasAnyExact(Tags);
}

bool UCSGameplayTagContainerExtensions::HasAll(const FGameplayTagContainer& Container, const FGameplayTagContainer& Tags)
{
	return Container.HasAll(Tags);
}

bool UCSGameplayTagContainerExtensions::HasAllExact(const FGameplayTagContainer& Container, const FGameplayTagContainer& Tags)
{
	return Container.HasAllExact(Tags);
}

int32 UCSGameplayTagContainerExtensions::Num(const FGameplayTagContainer& Container)
{
	return Container.Num();
}

bool UCSGameplayTagContainerExtensions::IsValid(const FGameplayTagContainer& Container)
{
	return Container.IsValid();
}

bool UCSGameplayTagContainerExtensions::IsEmpty(const FGameplayTagContainer& Container)
{
	return Container.IsEmpty();
}

FGameplayTagContainer UCSGameplayTagContainerExtensions::Filter(const FGameplayTagContainer& Container,
	const FGameplayTagContainer& Tags)
{
	return Container.Filter(Tags);
}

FGameplayTagContainer UCSGameplayTagContainerExtensions::FilterExact(const FGameplayTagContainer& Container,
	const FGameplayTagContainer& Tags)
{
	return Container.FilterExact(Tags);
}

bool UCSGameplayTagContainerExtensions::MatchesQuery(const FGameplayTagContainer& Container, const FGameplayTagQuery& Query)
{
	return Container.MatchesQuery(Query);
}

void UCSGameplayTagContainerExtensions::AppendTags(FGameplayTagContainer& Container, const FGameplayTagContainer& Tags)
{
	Container.AppendTags(Tags);
}

void UCSGameplayTagContainerExtensions::AppendMatchingTags(FGameplayTagContainer& Container, const FGameplayTagContainer& OtherA,
	const FGameplayTagContainer& OtherB)
{
	Container.AppendMatchingTags(OtherA, OtherB);
}

void UCSGameplayTagContainerExtensions::AddTag(FGameplayTagContainer& Container, const FGameplayTag& Tag)
{
	Container.AddTag(Tag);
}

void UCSGameplayTagContainerExtensions::AddTagFast(FGameplayTagContainer& Container, const FGameplayTag& Tag)
{
	Container.AddTagFast(Tag);
}

void UCSGameplayTagContainerExtensions::AddLeafTag(FGameplayTagContainer& Container, const FGameplayTag& Tag)
{
	Container.AddLeafTag(Tag);
}

void UCSGameplayTagContainerExtensions::RemoveTag(FGameplayTagContainer& Container, const FGameplayTag& Tag)
{
	Container.RemoveTag(Tag);
}

void UCSGameplayTagContainerExtensions::RemoveTags(FGameplayTagContainer& Container, const FGameplayTagContainer& Tags)
{
	Container.RemoveTags(Tags);
}

void UCSGameplayTagContainerExtensions::Reset(FGameplayTagContainer& Container)
{
	Container.Reset();
}

FGameplayTag UCSGameplayTagContainerExtensions::First(const FGameplayTagContainer& Container)
{
	return Container.First();
}

FGameplayTag UCSGameplayTagContainerExtensions::Last(const FGameplayTagContainer& Container)
{
	return Container.Last();
}

FString UCSGameplayTagContainerExtensions::ToString(const FGameplayTagContainer& Container)
{
	return Container.ToString();
}
