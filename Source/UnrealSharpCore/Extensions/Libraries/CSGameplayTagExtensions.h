#pragma once

#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include "CSGameplayTagExtensions.generated.h"

struct FGameplayTag;

UCLASS(meta = (Internal))
class UCSGameplayTagExtensions : public UBlueprintFunctionLibrary
{
	GENERATED_BODY()

public:

	/**
	 * Determine if this tag matches TagToCheck, expanding our parent tags
	 * "A.1".MatchesTag("A") will return True, "A".MatchesTag("A.1") will return False
	 * If TagToCheck is not Valid it will always return False
	 * 
	 * @return True if this tag matches TagToCheck
	 */
	UFUNCTION(meta=(ExtensionMethod, ScriptMethod))
	static bool MatchesTag(const FGameplayTag& Tag, const FGameplayTag& Other);

	/**
	 * Check to see how closely two FGameplayTags match. Higher values indicate more matching terms in the tags.
	 *
	 * @param TagToCheck	Tag to match against
	 *
	 * @return The depth of the match, higher means they are closer to an exact match
	 */
	UFUNCTION(meta=(ExtensionMethod, ScriptMethod))
	static int32 MatchesTagDepth(const FGameplayTag& Tag, const FGameplayTag& Other);

	/**
	 * Checks if this tag matches ANY of the tags in the specified container, also checks against our parent tags
	 * "A.1".MatchesAny({"A","B"}) will return True, "A".MatchesAny({"A.1","B"}) will return False
	 * If ContainerToCheck is empty/invalid it will always return False
	 *
	 * @return True if this tag matches ANY of the tags of in ContainerToCheck
	 */
	UFUNCTION(meta=(ExtensionMethod, ScriptMethod))
	static bool MatchesAny(const FGameplayTag& Tag, const FGameplayTagContainer& Tags);

	/**
	 * Checks if this tag matches ANY of the tags in the specified container, only allowing exact matches
	 * "A.1".MatchesAny({"A","B"}) will return False
	 * If ContainerToCheck is empty/invalid it will always return False
	 *
	 * @return True if this tag matches ANY of the tags of in ContainerToCheck exactly
	 */
	UFUNCTION(meta=(ExtensionMethod, ScriptMethod))
	static bool MatchesAnyExact(const FGameplayTag& Tag, const FGameplayTagContainer& Tags);
	
	/**
	 * Gets the FGameplayTag that corresponds to the TagName
	 *
	 * @param TagName The Name of the tag to search for
	 * @param ErrorIfNotfound: ensure() that tag exists.
	 * @return Will return the corresponding FGameplayTag or an empty one if not found.
	 */
	UFUNCTION(meta=(ScriptMethod))
	static FGameplayTag RequestGameplayTag(const FName TagName);
	
};
