using UnrealSharp.UnrealSharpCore;

namespace UnrealSharp.GameplayTags;

public partial record struct FGameplayTagQuery
{
    /// <summary>
    /// Returns true if the given tags match this query, or false otherwise. An empty query never matches.
    /// </summary>
    public bool Matches(FGameplayTagContainer tags) => UCSGameplayTagQueryExtensions.Matches(this, tags);

    /// <summary>
    /// Returns true if this query is empty, false otherwise.
    /// </summary>
    public bool IsEmpty => UCSGameplayTagQueryExtensions.IsEmpty(this);

    /// <summary>
    /// Resets this query to its default empty state.
    /// </summary>
    public void Clear() => UCSGameplayTagQueryExtensions.Clear(ref this);

    /// <summary>
    /// Replaces existing tags with passed in tags. Does not modify the tag query expression logic.
    /// Useful when you need to cache off and update an often used query. Must use same sized tag container!
    /// </summary>
    public void ReplaceTagsFast(FGameplayTagContainer tags) => UCSGameplayTagQueryExtensions.ReplaceTagsFast(ref this, tags);

    /// <summary>
    /// Replaces the existing tag with the passed in tag. Does not modify the tag query expression logic.
    /// The query must reference exactly one tag!
    /// </summary>
    public void ReplaceTagFast(FGameplayTag tag) => UCSGameplayTagQueryExtensions.ReplaceTagFast(ref this, tag);

    /// <summary>
    /// Sets the user-specified description of this query.
    /// </summary>
    public void SetUserDescription(string userDescription) => UCSGameplayTagQueryExtensions.SetUserDescription(ref this, userDescription);

    /// <summary>
    /// The user description if set, otherwise the auto-generated description.
    /// </summary>
    public string Description => UCSGameplayTagQueryExtensions.GetDescription(this);

    /// <summary>
    /// The explicit list of all unique gameplay tags referenced by the query.
    /// </summary>
    public IList<FGameplayTag> GameplayTagArray => UCSGameplayTagQueryExtensions.GetGameplayTagArray(this);

    /// <summary>
    /// Creates a tag query that will match if there are any common tags between the given tags and the tags being queried against.
    /// </summary>
    public static FGameplayTagQuery MakeQuery_MatchAnyTags(FGameplayTagContainer tags) => UCSGameplayTagQueryExtensions.MakeQuery_MatchAnyTags(tags);

    /// <summary>
    /// Creates a tag query that will match if the tags being queried against have all of the given tags.
    /// </summary>
    public static FGameplayTagQuery MakeQuery_MatchAllTags(FGameplayTagContainer tags) => UCSGameplayTagQueryExtensions.MakeQuery_MatchAllTags(tags);

    /// <summary>
    /// Creates a tag query that will match if the tags being queried against have none of the given tags.
    /// </summary>
    public static FGameplayTagQuery MakeQuery_MatchNoTags(FGameplayTagContainer tags) => UCSGameplayTagQueryExtensions.MakeQuery_MatchNoTags(tags);

    /// <summary>
    /// Creates a tag query that will match if any of the given tags is exactly in the tags being queried against.
    /// </summary>
    public static FGameplayTagQuery MakeQuery_ExactMatchAnyTags(FGameplayTagContainer tags) => UCSGameplayTagQueryExtensions.MakeQuery_ExactMatchAnyTags(tags);

    /// <summary>
    /// Creates a tag query that will match if all of the given tags are exactly in the tags being queried against.
    /// </summary>
    public static FGameplayTagQuery MakeQuery_ExactMatchAllTags(FGameplayTagContainer tags) => UCSGameplayTagQueryExtensions.MakeQuery_ExactMatchAllTags(tags);

    /// <summary>
    /// Creates a tag query that will match if the tags being queried against have the given tag.
    /// </summary>
    public static FGameplayTagQuery MakeQuery_MatchTag(FGameplayTag tag) => UCSGameplayTagQueryExtensions.MakeQuery_MatchTag(tag);

    public bool Equals(FGameplayTagQuery other) => UCSGameplayTagQueryExtensions.Identical(this, other);

    public override int GetHashCode()
    {
        HashCode hash = new();
        foreach (FGameplayTag tag in GameplayTagArray)
        {
            hash.Add(tag);
        }

        return hash.ToHashCode();
    }
}
