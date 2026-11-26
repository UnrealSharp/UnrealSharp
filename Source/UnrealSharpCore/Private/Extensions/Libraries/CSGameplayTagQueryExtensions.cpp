#include "Extensions/Libraries/CSGameplayTagQueryExtensions.h"
#include "GameplayTagContainer.h"

bool UCSGameplayTagQueryExtensions::Matches(const FGameplayTagQuery& Query, const FGameplayTagContainer& Tags)
{
	return Query.Matches(Tags);
}

bool UCSGameplayTagQueryExtensions::IsEmpty(const FGameplayTagQuery& Query)
{
	return Query.IsEmpty();
}

void UCSGameplayTagQueryExtensions::Clear(FGameplayTagQuery& Query)
{
	Query.Clear();
}

void UCSGameplayTagQueryExtensions::ReplaceTagsFast(FGameplayTagQuery& Query, const FGameplayTagContainer& Tags)
{
	Query.ReplaceTagsFast(Tags);
}

void UCSGameplayTagQueryExtensions::ReplaceTagFast(FGameplayTagQuery& Query, const FGameplayTag& Tag)
{
	Query.ReplaceTagFast(Tag);
}

void UCSGameplayTagQueryExtensions::SetUserDescription(FGameplayTagQuery& Query, const FString& UserDescription)
{
	Query.SetUserDescription(UserDescription);
}

FString UCSGameplayTagQueryExtensions::GetDescription(const FGameplayTagQuery& Query)
{
	return Query.GetDescription();
}

TArray<FGameplayTag> UCSGameplayTagQueryExtensions::GetGameplayTagArray(const FGameplayTagQuery& Query)
{
	return Query.GetGameplayTagArray();
}

bool UCSGameplayTagQueryExtensions::Identical(const FGameplayTagQuery& Query, const FGameplayTagQuery& Other)
{
	return Query == Other;
}

FGameplayTagQuery UCSGameplayTagQueryExtensions::MakeQuery_MatchAnyTags(const FGameplayTagContainer& Tags)
{
	return FGameplayTagQuery::MakeQuery_MatchAnyTags(Tags);
}

FGameplayTagQuery UCSGameplayTagQueryExtensions::MakeQuery_MatchAllTags(const FGameplayTagContainer& Tags)
{
	return FGameplayTagQuery::MakeQuery_MatchAllTags(Tags);
}

FGameplayTagQuery UCSGameplayTagQueryExtensions::MakeQuery_MatchNoTags(const FGameplayTagContainer& Tags)
{
	return FGameplayTagQuery::MakeQuery_MatchNoTags(Tags);
}

FGameplayTagQuery UCSGameplayTagQueryExtensions::MakeQuery_ExactMatchAnyTags(const FGameplayTagContainer& Tags)
{
	return FGameplayTagQuery::MakeQuery_ExactMatchAnyTags(Tags);
}

FGameplayTagQuery UCSGameplayTagQueryExtensions::MakeQuery_ExactMatchAllTags(const FGameplayTagContainer& Tags)
{
	return FGameplayTagQuery::MakeQuery_ExactMatchAllTags(Tags);
}

FGameplayTagQuery UCSGameplayTagQueryExtensions::MakeQuery_MatchTag(const FGameplayTag& Tag)
{
	return FGameplayTagQuery::MakeQuery_MatchTag(Tag);
}
