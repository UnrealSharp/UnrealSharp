using System.Reflection;
using System.Runtime.CompilerServices;
using UnrealSharp.Core;
using UnrealSharp.UnrealSharpCore;

namespace UnrealSharp.GameplayTags;

public partial struct FGameplayTag
{
    /// <summary>
    /// Registers a gameplay tag tracked by the calling assembly for editor cleanup during hot reload.
    /// If the tag exists, it returns the existing tag. 
    /// NOTE: Due to initialization timing, this tag is not discoverable via the static class 
    /// 'GameplayTags', the returned value needs to be stored for later use.
    /// </summary>
    /// <param name="tagName">The name of the tag (e.g., "Character.Hero").</param>
    /// <param name="tagComment">Optional comment about the tag.</param>
    /// <returns>The newly registered or existing FGameplayTag.</returns>
#if WITH_EDITOR
    [MethodImpl(MethodImplOptions.NoInlining)]
    public FGameplayTag(string tagName, string tagComment = "")
    {
        Assembly assembly = Assembly.GetCallingAssembly();
        this = UCSGameplayTagsManager.AddTag_Editor(assembly.GetName().Name, tagName, tagComment);
    }
#else
    public FGameplayTag(string tagName, string tagComment = "")
    {
        this = UCSGameplayTagsManager.AddTag_Runtime(tagName, tagComment);
    }
#endif

    /// <summary>
    /// Returns empty GameplayTag
    /// </summary>
    public static FGameplayTag None => new()
    {
        TagName = FName.None
    };
    
    /// <summary>
    /// Check if this tag is exactly the same as TagToCheck
    /// </summary>
    /// <param name="tagToCheck">The tag to check against</param>
    /// <returns>True if this tag matches TagToCheck</returns>
    public bool MatchesTagExact(FGameplayTag tagToCheck) 
    {
        return TagName == tagToCheck.TagName;
    }
    
    /// <summary>
    /// Is tag valid?
    /// </summary>
    /// <returns>True if tag is valid</returns>
    public bool IsValid => TagName.IsValid;

    /// <summary>
    /// Parses the tag name and returns the name of the leaf.
    /// For example, calling this on x.y.z would return the z component.
    /// </summary>
    //public FName LeafName => this.GetTagLeafName();
    
    public bool Equals(FGameplayTag other)
    {
        return TagName.Equals(other.TagName);
    }

    public override bool Equals(object? obj)
    {
        return obj is FGameplayTag other && Equals(other);
    }

    public override int GetHashCode()
    {
        return TagName.GetHashCode();
    }

    public override string ToString()
    {
        return TagName.ToString();
    }
    
    public static bool operator == (FGameplayTag lhs, FGameplayTag rhs)
    {
        return lhs.TagName == rhs.TagName;
    }

    public static bool operator !=(FGameplayTag lhs, FGameplayTag rhs)
    {
        return !(lhs == rhs);
    }
    
    public static implicit operator FGameplayTag(string tagName)
    {
        return new FGameplayTag(tagName);
    }
    
    public static implicit operator FGameplayTag(FName gameplayTag)
    {
        return new FGameplayTag(gameplayTag);
    }
}