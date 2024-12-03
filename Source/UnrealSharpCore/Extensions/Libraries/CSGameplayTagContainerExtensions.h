#pragma once

#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include "CSGameplayTagContainerExtensions.generated.h"

struct FGameplayTag;
struct FGameplayTagContainer;

UCLASS(meta = (Internal))
class UCSGameplayTagContainerExtensions : public UBlueprintFunctionLibrary
{
	GENERATED_BODY()

public:

	/**
	 * Determine if TagToCheck is present in this container, also checking against parent tags
	 * {"A.1"}.HasTag("A") will return True, {"A"}.HasTag("A.1") will return False
	 * If TagToCheck is not Valid it will always return False
	 * 
	 * @return True if TagToCheck is in this container, false if it is not
	 */
	UFUNCTION(meta=(ExtensionMethod, ScriptMethod))
	static bool HasTag(const FGameplayTagContainer& Container, const FGameplayTag& Tag);

	/**
	 * Determine if TagToCheck is explicitly present in this container, only allowing exact matches
	 * {"A.1"}.HasTagExact("A") will return False
	 * If TagToCheck is not Valid it will always return False
	 * 
	 * @return True if TagToCheck is in this container, false if it is not
	 */
	UFUNCTION(meta=(ExtensionMethod, ScriptMethod))
	static bool HasTagExact(const FGameplayTagContainer& Container, const FGameplayTag& Tag);

	/**
	 * Checks if this container contains ANY of the tags in the specified container, also checks against parent tags
	 * {"A.1"}.HasAny({"A","B"}) will return True, {"A"}.HasAny({"A.1","B"}) will return False
	 * If ContainerToCheck is empty/invalid it will always return False
	 *
	 * @return True if this container has ANY of the tags of in ContainerToCheck
	 */
	UFUNCTION(meta=(ExtensionMethod, ScriptMethod))
	static bool HasAny(const FGameplayTagContainer& Container, const FGameplayTagContainer& Tags);

		/**
	 * Checks if this container contains ANY of the tags in the specified container, only allowing exact matches
	 * {"A.1"}.HasAny({"A","B"}) will return False
	 * If ContainerToCheck is empty/invalid it will always return False
	 *
	 * @return True if this container has ANY of the tags of in ContainerToCheck
	 */
	UFUNCTION(meta=(ExtensionMethod, ScriptMethod))
	static bool HasAnyExact(const FGameplayTagContainer& Container, const FGameplayTagContainer& Tags);

	/**
	 * Checks if this container contains ALL of the tags in the specified container, also checks against parent tags
	 * {"A.1","B.1"}.HasAll({"A","B"}) will return True, {"A","B"}.HasAll({"A.1","B.1"}) will return False
	 * If ContainerToCheck is empty/invalid it will always return True, because there were no failed checks
	 *
	 * @return True if this container has ALL of the tags of in ContainerToCheck, including if ContainerToCheck is empty
	 */
	UFUNCTION(meta=(ExtensionMethod, ScriptMethod))
	static bool HasAll(const FGameplayTagContainer& Container, const FGameplayTagContainer& Tags);

	/**
	 * Checks if this container contains ALL of the tags in the specified container, only allowing exact matches
	 * {"A.1","B.1"}.HasAll({"A","B"}) will return False
	 * If ContainerToCheck is empty/invalid it will always return True, because there were no failed checks
	 *
	 * @return True if this container has ALL of the tags of in ContainerToCheck, including if ContainerToCheck is empty
	 */
	UFUNCTION(meta=(ExtensionMethod, ScriptMethod))
	static bool HasAllExact(const FGameplayTagContainer& Container, const FGameplayTagContainer& Tags);

	/** Returns the number of explicitly added tags */
	UFUNCTION(meta=(ScriptMethod))
	static int32 Num(const FGameplayTagContainer& Container);

	/** Returns whether the container has any valid tags */
	UFUNCTION(meta=(ScriptMethod))
	static bool IsValid(const FGameplayTagContainer& Container);

	/** Returns true if container is empty */
	UFUNCTION(meta=(ScriptMethod))
	static bool IsEmpty(const FGameplayTagContainer& Container);

	/**
	 * Returns a filtered version of this container, returns all tags that match against any of the tags in OtherContainer, expanding parents
	 *
	 * @param OtherContainer		The Container to filter against
	 *
	 * @return A FGameplayTagContainer containing the filtered tags
	 */
	UFUNCTION(meta=(ExtensionMethod, ScriptMethod))
	static FGameplayTagContainer Filter(const FGameplayTagContainer& Container, const FGameplayTagContainer& Tags);

	/**
	 * Returns a filtered version of this container, returns all tags that match exactly one in OtherContainer
	 *
	 * @param OtherContainer		The Container to filter against
	 *
	 * @return A FGameplayTagContainer containing the filtered tags
	 */
	UFUNCTION(meta=(ExtensionMethod, ScriptMethod))
	static FGameplayTagContainer FilterExact(const FGameplayTagContainer& Container, const FGameplayTagContainer& Tags);

	/** 
	 * Checks if this container matches the given query.
	 *
	 * @param Query		Query we are checking against
	 *
	 * @return True if this container matches the query, false otherwise.
	 */
	UFUNCTION(meta=(ExtensionMethod, ScriptMethod))
	static bool MatchesQuery(const FGameplayTagContainer& Container, const FGameplayTagQuery& Query);

	/** 
	 * Adds all the tags from one container to this container 
	 * NOTE: From set theory, this effectively is the union of the container this is called on with Other.
	 *
	 * @param Other TagContainer that has the tags you want to add to this container 
	 */
	UFUNCTION(meta=(ScriptMethod))
	static void AppendTags(UPARAM(ref) FGameplayTagContainer& Container, const FGameplayTagContainer& Tags);

	/** 
	 * Adds all the tags that match between the two specified containers to this container.  WARNING: This matches any
	 * parent tag in A, not just exact matches!  So while this should be the union of the container this is called on with
	 * the intersection of OtherA and OtherB, it's not exactly that.  Since OtherB matches against its parents, any tag
	 * in OtherA which has a parent match with a parent of OtherB will count.  For example, if OtherA has Color.Green
	 * and OtherB has Color.Red, that will count as a match due to the Color parent match!
	 * If you want an exact match, you need to call A.FilterExact(B) (above) to get the intersection of A with B.
	 * If you need the disjunctive union (the union of two sets minus their intersection), use AppendTags to create
	 * Union, FilterExact to create Intersection, and then call Union.RemoveTags(Intersection).
	 *
	 * @param OtherA TagContainer that has the matching tags you want to add to this container, these tags have their parents expanded
	 * @param OtherB TagContainer used to check for matching tags.  If the tag matches on any parent, it counts as a match.
	 */
	UFUNCTION(meta=(ScriptMethod))
	static void AppendMatchingTags(UPARAM(ref) FGameplayTagContainer& Container, const FGameplayTagContainer& OtherA, const FGameplayTagContainer& OtherB);

	/**
	 * Add the specified tag to the container
	 *
	 * @param TagToAdd Tag to add to the container
	 */
	UFUNCTION(meta=(ScriptMethod))
	static void AddTag(UPARAM(ref) FGameplayTagContainer& Container, const FGameplayTag& Tag);

	/**
	 * Add the specified tag to the container without checking for uniqueness
	 *
	 * @param TagToAdd Tag to add to the container
	 * 
	 * Useful when building container from another data struct (TMap for example)
	 */
	UFUNCTION(meta=(ScriptMethod))
	static void AddTagFast(UPARAM(ref) FGameplayTagContainer& Container, const FGameplayTag& Tag);

	/**
	 * Adds a tag to the container and removes any direct parents, wont add if child already exists
	 *
	 * @param Tag			The tag to try and add to this container
	 * 
	 * @return True if tag was added
	 */
	UFUNCTION(meta=(ScriptMethod))
	static void AddLeafTag(UPARAM(ref) FGameplayTagContainer& Container, const FGameplayTag& Tag);

	/**
	 * Tag to remove from the container
	 * 
	 * @param TagToRemove		Tag to remove from the container
	 * @param bDeferParentTags	Skip calling FillParentTags for performance (must be handled by calling code)
	 */
	UFUNCTION(meta=(ScriptMethod))
	static void RemoveTag(UPARAM(ref) FGameplayTagContainer& Container, const FGameplayTag& Tag);

	/**
	 * Removes all tags in TagsToRemove from this container
	 *
	 * @param TagsToRemove	Tags to remove from the container
	 */
	UFUNCTION(meta=(ScriptMethod))
	static void RemoveTags(UPARAM(ref) FGameplayTagContainer& Container, const FGameplayTagContainer& Tags);

	/** Remove all tags from the container. Will maintain slack by default */
	UFUNCTION(meta=(ScriptMethod))
	static void Reset(UPARAM(ref) FGameplayTagContainer& Container);

	/**
	 * Returns the first tag in the container
	 *
	 * @return The first tag in the container
	 */
	UFUNCTION(BlueprintCallable)
	static FGameplayTag First(const FGameplayTagContainer& Container);
	
	/**
	 * Returns the last tag in the container
	 *
	 * @return The last tag in the container
	 */
	UFUNCTION(BlueprintCallable)
	static FGameplayTag Last(const FGameplayTagContainer& Container);

	/**
	 * Returns a string representation of the container
	 *
	 * @return A string representation of the container
	 */
	UFUNCTION(BlueprintCallable)
	static FString ToString(const FGameplayTagContainer& Container);
	
};
