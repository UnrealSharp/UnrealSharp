using System.Runtime.InteropServices;
using UnrealSharp.CSharpForUE;

namespace UnrealSharp.GameplayTags;

[StructLayout(LayoutKind.Sequential)]
public partial struct FGameplayTagContainer
{
    public FGameplayTagContainer(FGameplayTag[] tags, FGameplayTag[]? parentTags = null)
    {
        GameplayTags = tags ?? Array.Empty<FGameplayTag>();
        ParentTags = parentTags ?? Array.Empty<FGameplayTag>();
    }

    public FGameplayTagContainer(IList<FGameplayTag> tags, IList<FGameplayTag>? parentTags = null) 
        : this(tags?.ToArray() ?? Array.Empty<FGameplayTag>(), parentTags?.ToArray())
    {
    }

    public FGameplayTagContainer(params FGameplayTag[] tags) 
        : this(tags, null)
    {
    }

    public FGameplayTagContainer(FGameplayTag tag)
        : this([tag])
    {
    }
    
    /// <summary>
    /// Adds all the tags from one container to this container.
    /// NOTE: From set theory, this effectively is the union of the container this is called on with Other.
    /// </summary>
    public void AppendTags(FGameplayTagContainer other) => UCSGameplayTagContainerExtensions.AppendTags(ref this, other);
    
    /// <summary>
    /// Adds all the tags from a list to this container.
    /// </summary>
    public void AppendTags(IList<FGameplayTag> other)
    {
        foreach (FGameplayTag tag in other)
        {
            AddTag(tag);
        }
    }

    /// <summary>
    /// Adds all the tags from a params array to this container.
    /// </summary>
    public void AppendTags(params FGameplayTag[] other)
    {
        AppendTags((IList<FGameplayTag>)other);
    }
    
    /// <summary>
    /// Adds all the tags that match between the two specified containers to this container.  WARNING: This matches any
    /// parent tag in A, not just exact matches!  So while this should be the union of the container this is called on with
    /// the intersection of OtherA and OtherB, it's not exactly that.  Since OtherB matches against its parents, any tag
    /// in OtherA which has a parent match with a parent of OtherB will count.  For example, if OtherA has Color.Green
    /// and OtherB has Color.Red, that will count as a match due to the Color parent match!
    /// If you want an exact match, you need to call A.FilterExact(B) (above) to get the intersection of A with B.
    /// If you need the disjunctive union (the union of two sets minus their intersection), use AppendTags to create
    /// Union, FilterExact to create Intersection, and then call Union.RemoveTags(Intersection).
    /// </summary>
    public void AppendMatchingTags(FGameplayTagContainer otherA, FGameplayTagContainer otherB) => UCSGameplayTagContainerExtensions.AppendMatchingTags(ref this, otherA, otherB);
    
    /// <summary>
    /// Add the specified tag to the container
    /// </summary>
    public void AddTag(FGameplayTag tag) => UCSGameplayTagContainerExtensions.AddTag(ref this, tag);
    
    /// <summary>
    /// Add the specified tag to the container without checking for uniqueness
    /// </summary>
    public void AddTagFast(FGameplayTag tag) => UCSGameplayTagContainerExtensions.AddTagFast(ref this, tag);
    
    /// <summary>
    /// Adds a tag to the container and removes any direct parents, wont add if child already exists
    /// </summary>
    public void AddLeafTag(FGameplayTag tag) => UCSGameplayTagContainerExtensions.AddLeafTag(ref this, tag);
    
    /// <summary>
    /// Tag to remove from the container
    /// </summary>
    public void RemoveTag(FGameplayTag tag) => UCSGameplayTagContainerExtensions.RemoveTag(ref this, tag);
    
    /// <summary>
    /// Removes all tags in TagsToRemove from this container
    /// </summary>
    public void RemoveTags(FGameplayTagContainer tagsToRemove) => UCSGameplayTagContainerExtensions.RemoveTags(ref this, tagsToRemove);
    
    /// <summary>
    /// Remove all tags from the container. Will maintain slack by default
    /// </summary>
    public void Reset() => UCSGameplayTagContainerExtensions.Reset(ref this);
    
    /// <summary>
    /// Determine if TagToCheck is present in this container, also checking against parent tags
    /// {"A.1"}.HasTag("A") will return True, {"A"}.HasTag("A.1") will return False
    /// If TagToCheck is not Valid it will always return False
    /// </summary>
    public bool HasTag(FGameplayTag tag) => UCSGameplayTagContainerExtensions.HasTag(this, tag);
    
    /// <summary>
    /// The count of gameplay tags in this container
    /// </summary>
    public int Count => UCSGameplayTagContainerExtensions.Num(this);

    /// <summary>
    /// Find the first gameplay tag in this container
    /// </summary>
    /// <returns>The first gameplay tag in this container</returns>
    public FGameplayTag First => UCSGameplayTagContainerExtensions.First(this);
    
    /// <summary>
    /// Find the last gameplay tag in this container
    /// </summary>
    /// <returns>The last gameplay tag in this container</returns>
    public FGameplayTag Last => UCSGameplayTagContainerExtensions.Last(this);
    
    /// <summary>
    /// Check if the container is empty
    /// </summary>
    /// <returns>True if the container is empty</returns>
    public bool IsEmpty => UCSGameplayTagContainerExtensions.IsEmpty(this);

    /// <summary>
    /// Check if the container is valid
    /// </summary>
    /// <returns>True if the container is valid</returns>
    public bool IsValid => UCSGameplayTagContainerExtensions.IsValid(this);
    
    public override string ToString()
    {
        return UCSGameplayTagContainerExtensions.ToString(this);
    }
}