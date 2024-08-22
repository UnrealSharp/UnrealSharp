using System.Runtime.InteropServices;
using UnrealSharp.Interop;

namespace UnrealSharp.GameplayTags;

[StructLayout(LayoutKind.Sequential)]
public partial struct GameplayTagContainer
{
    internal UnmanagedArray _gameplayTags;
    internal UnmanagedArray _parentTags;

    /// <summary>
    /// Returns the number of explicitly added tags
    /// </summary>
    public int Count => _gameplayTags.ArrayNum;
    
    /// <summary>
    /// Returns true if container is empty of tags
    /// </summary>
    public bool IsEmpty => Count == 0;
    
    /// <summary>
    /// Returns whether the container has any valid tags
    /// </summary>
    public bool IsValid => Count > 0;
    
    /// <summary>
    /// Determine if TagToCheck is present in this container, also checking against parent tags
    /// {"A.1"}.HasTag("A") will return True, {"A"}.HasTag("A.1") will return False
    /// If TagToCheck is not Valid it will always return False
    /// </summary>
    /// <returns>True if TagToCheck is in this container, false if it is not</returns>
    public bool HasTag(FGameplayTag tag)
    {
        return FGameplayTagContainerExporter.CallHasTag(ref this, ref tag);
    }
    
    /// <summary>
    /// Determine if TagToCheck is explicitly present in this container, only allowing exact matches
    /// {"A.1"}.HasTagExact("A") will return False
    /// If TagToCheck is not Valid it will always return False
    /// </summary>
    /// <returns>True if TagToCheck is in this container, false if it is not</returns>
    public bool HasTagExact(FGameplayTag tag)
    {
        return FGameplayTagContainerExporter.CallHasTagExact(ref this, ref tag);
    }
    
    /// <summary>
    /// Checks if this container contains ANY of the tags in the specified container, also checks against parent tags
    /// {"A.1"}.HasAny({"A","B"}) will return True, {"A"}.HasAny({"A.1","B"}) will return False
    /// If ContainerToCheck is empty/invalid it will always return False
    /// </summary>
    /// <returns>True if this container has ANY of the tags of in ContainerToCheck</returns>
    public bool HasAny(GameplayTagContainer other)
    {
        return FGameplayTagContainerExporter.CallHasAny(ref this, ref other);
    }
    
    /// <summary>
    /// Checks if this container contains ANY of the tags in the specified container, only allowing exact matches
    /// {"A.1"}.HasAny({"A","B"}) will return False
    /// If ContainerToCheck is empty/invalid it will always return False
    /// </summary>
    /// <returns>True if this container has ANY of the tags of in ContainerToCheck</returns>
    public bool HasAnyExact(GameplayTagContainer other)
    {
        return FGameplayTagContainerExporter.CallHasAnyExact(ref this, ref other);
    }
    
    /// <summary>
    /// Checks if this container contains ALL of the tags in the specified container, also checks against parent tags
    /// {"A.1","B.1"}.HasAll({"A","B"}) will return True, {"A","B"}.HasAll({"A.1","B.1"}) will return False
    /// If ContainerToCheck is empty/invalid it will always return True, because there were no failed checks
    /// </summary>
    /// <returns>True if this container has ALL of the tags of in ContainerToCheck, including if ContainerToCheck is empty</returns>
    public bool HasAll(GameplayTagContainer other)
    {
        return FGameplayTagContainerExporter.CallHasAll(ref this, ref other);
    }
    
    /// <summary>
    /// Checks if this container contains ALL of the tags in the specified container, only allowing exact matches
    /// {"A.1","B.1"}.HasAll({"A","B"}) will return False
    /// If ContainerToCheck is empty/invalid it will always return True, because there were no failed checks
    /// </summary>
    /// <returns>True if this container has ALL of the tags of in ContainerToCheck, including if ContainerToCheck is empty</returns>
    public bool HasAllExact(GameplayTagContainer other)
    {
        return FGameplayTagContainerExporter.CallHasAllExact(ref this, ref other);
    }
    
    /// <summary>
    /// Add the specified tag to the container
    /// </summary>
    /// <param name="tag">Tag to add to the container</param>
    public void AddTag(FGameplayTag tag)
    {
        FGameplayTagContainerExporter.CallAddTag(ref this, ref tag);
    }
    
    /// <summary>
    /// Add the specified tag to the container without checking for uniqueness
    /// Useful when building container from another data struct (TMap for example)
    /// </summary>
    /// <param name="tag">Tag to add to the container</param>
    public void AddTagFast(FGameplayTag tag)
    {
        FGameplayTagContainerExporter.CallAddTagFast(ref this, ref tag);
    }
    
    /// <summary>
    /// Adds a tag to the container and removes any direct parents, wont add if child already exists
    /// </summary>
    /// <param name="tag">The tag to try and add to this container</param>
    public bool AddLeafTag(FGameplayTag tag)
    {
        return FGameplayTagContainerExporter.CallAddLeafTag(ref this, ref tag);
    }

    /// <summary>
    /// Tag to remove from the container
    /// </summary>
    /// <param name="tag"></param>
    /// <param name="bDeferParentTags"></param>
    public bool RemoveTag(FGameplayTag tag, bool bDeferParentTags = false)
    {
        return FGameplayTagContainerExporter.CallRemoveTag(ref this, ref tag, bDeferParentTags);
    }
    
    /// <summary>
    /// Removes all tags in TagsToRemove from this container
    /// </summary>
    public void RemoveTags(GameplayTagContainer other)
    {
        FGameplayTagContainerExporter.CallRemoveTags(ref this, ref other);
    }
    
    
    /// <summary>
    /// Remove all tags from the container. Will maintain slack by default
    /// </summary>
    public void Reset()
    {
        FGameplayTagContainerExporter.CallReset(ref this);
    }
    
    /// <summary>
    /// Adds all the tags from one container to this container
    /// NOTE: From set theory, this effectively is the union of the container this is called on with Other.
    /// </summary>
    /// <param name="other">TagContainer that has the tags you want to add to this container </param>
    public void AppendTags(GameplayTagContainer other)
    {
        FGameplayTagContainerExporter.CallAppendTags(ref this, ref other);
    }
    
    /// <summary>
    /// Returns a filtered version of this container, returns all tags that match against any of the tags in OtherContainer, expanding parents
    /// </summary>
    /// <param name="other">The Container to filter against</param>
    /// <returns>A FGameplayTagContainer containing the filtered tags</returns>
    public GameplayTagContainer Filter(GameplayTagContainer other)
    {
        return FGameplayTagContainerExporter.CallFilter(ref this, ref other);
    }
    
    /// <summary>
    /// Returns a filtered version of this container, returns all tags that match exactly one in OtherContainer
    /// </summary>
    /// <param name="other">The Container to filter against</param>
    /// <returns>A FGameplayTagContainer containing the filtered tags</returns>
    public GameplayTagContainer FilterExact(GameplayTagContainer other)
    {
        return FGameplayTagContainerExporter.CallFilterExact(ref this, ref other);
    }
    
    /// <summary>
    /// Returns string version of container in ImportText format
    /// </summary>
    public override string ToString()
    {
        unsafe
        {
            UnmanagedArray buffer = new UnmanagedArray();
            try
            {
                FGameplayTagContainerExporter.CallToString(ref this, ref _gameplayTags);
                return new string((char*)buffer.Data);
            }
            finally
            {
                buffer.Destroy();
            }
        }
    }
}